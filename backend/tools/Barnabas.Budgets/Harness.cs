using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using Barnabas.Domain.Members;
using Barnabas.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Barnabas.Budgets;

/// <summary>
/// Measures what <c>L2-103</c> and <c>L2-107</c> ask about, and says what it measured.
/// </summary>
/// <remarks>
/// Every run warms up before it measures. The first request to an endpoint pays for a JIT, a
/// connection, and a query plan, and including that in a percentile measures the start-up rather
/// than the product.
/// <para>
/// Percentiles rather than an average, because the requirements are written as percentiles and
/// because an average hides exactly the tail they are about.
/// </para>
/// </remarks>
public static class Harness
{
    private const int ListingsPerCongregation = 500;
    private const int CongregationsForConcurrency = 50;
    private const int SamplesPerEndpoint = 60;
    private const int WarmUpRequests = 10;

    public static async Task<int> RunAsync(string[] args)
    {
        var settings = Settings.From(args);

        Console.WriteLine("Barnabas budgets");
        Console.WriteLine($"  api      {settings.BaseAddress}");
        Console.WriteLine($"  database {Redact(settings.ConnectionString)}");
        Console.WriteLine();

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(20));
        var token = cancellation.Token;

        var population = new Population(settings.ConnectionString);
        var issuer = Issuer(settings);

        var measurements = new List<Measurement>();

        try
        {
            Console.WriteLine(
                $"Arranging one congregation of {ListingsPerCongregation} listings...");

            var one = await population.ArrangeAsync(
                congregations: 1,
                listingsEach: ListingsPerCongregation,
                issueToken: (member, session) => issuer.Issue(member, session).Value,
                cancellationToken: token);

            measurements.AddRange(await AtScaleAsync(settings, one[0], token));

            Console.WriteLine();
            Console.WriteLine(
                $"Arranging {CongregationsForConcurrency} congregations...");

            // Fewer listings each: L2-107 is about many congregations at once, and half a million
            // rows would be measuring the developer's disk.
            var many = await population.ArrangeAsync(
                congregations: CongregationsForConcurrency,
                listingsEach: 40,
                issueToken: (member, session) => issuer.Issue(member, session).Value,
                cancellationToken: token);

            measurements.AddRange(await AcrossCongregationsAsync(settings, many, token));
        }
        finally
        {
            // Whatever happened, the acceptance suites should not find five hundred listings of
            // somebody else's next time they run.
            await population.RemoveAsync(CancellationToken.None);
        }

        Console.WriteLine();
        Console.WriteLine("Results");

        foreach (var measurement in measurements)
        {
            Console.WriteLine(measurement);
        }

        var missed = measurements.Count(measurement => !measurement.Met);

        Console.WriteLine();
        Console.WriteLine(missed == 0
            ? $"All {measurements.Count} budgets met."
            : $"{missed} of {measurements.Count} budgets missed.");

        return missed == 0 ? 0 : 1;
    }

    /// <summary>L2-103: the board, a search, and a write, against a congregation of 500 listings.</summary>
    private static async Task<IReadOnlyList<Measurement>> AtScaleAsync(
        Settings settings,
        Population.Parish parish,
        CancellationToken cancellationToken)
    {
        using var client = ClientFor(settings, parish.AccessToken);

        return
        [
            await MeasureAsync(
                new Budget("the board at 500 listings", "L2-103 AC1", settings.BoardMilliseconds),
                () => client.GetAsync("/board", cancellationToken),
                cancellationToken),

            await MeasureAsync(
                new Budget("a search at 500 listings", "L2-103 AC2", settings.SearchMilliseconds),
                () => client.GetAsync("/search?term=ladder", cancellationToken),
                cancellationToken),

            await MeasureAsync(
                new Budget("posting a listing", "L2-103 AC3", settings.WriteMilliseconds),
                () => client.PostAsJsonAsync(
                    "/listings/give",
                    new
                    {
                        title = "A thing measured under load",
                        description = "Posted by the budget harness.",
                        category = "Tools",
                        neighbourhood = "Riverdale",
                    },
                    cancellationToken),
                cancellationToken),
        ];
    }

    /// <summary>
    /// L2-107: the same budgets with fifty congregations active, and one parish's noise measured
    /// against another's quiet.
    /// </summary>
    private static async Task<IReadOnlyList<Measurement>> AcrossCongregationsAsync(
        Settings settings,
        IReadOnlyList<Population.Parish> parishes,
        CancellationToken cancellationToken)
    {
        var measured = parishes[0];
        var noisy = parishes[1];

        // AC1: load across all of them, and the board still answers within budget.
        using var everyone = new SemaphoreSlim(1);

        var readers = parishes.Select(parish => Task.Run(
            async () =>
            {
                using var client = ClientFor(settings, parish.AccessToken);

                for (var call = 0; call < 20 && !cancellationToken.IsCancellationRequested; call++)
                {
                    using var response = await client.GetAsync("/board", cancellationToken);
                }
            },
            cancellationToken)).ToList();

        Measurement underLoad;

        using (var client = ClientFor(settings, measured.AccessToken))
        {
            underLoad = await MeasureAsync(
                new Budget(
                    $"the board with {parishes.Count} congregations active",
                    "L2-107 AC1",
                    settings.BoardMilliseconds),
                () => client.GetAsync("/board", cancellationToken),
                cancellationToken);
        }

        await Task.WhenAll(readers);

        // AC2: one congregation generating heavy load, another measured while it does.
        using var storm = new CancellationTokenSource();
        using var both = CancellationTokenSource.CreateLinkedTokenSource(storm.Token, cancellationToken);

        var heavy = Enumerable.Range(0, 8).Select(_ => Task.Run(
            async () =>
            {
                using var client = ClientFor(settings, noisy.AccessToken);

                while (!both.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var response = await client.GetAsync("/board", both.Token);
                        using var searched = await client.GetAsync("/search?term=a", both.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            },
            both.Token)).ToList();

        Measurement beside;

        using (var client = ClientFor(settings, measured.AccessToken))
        {
            beside = await MeasureAsync(
                new Budget(
                    "another congregation's board beside a noisy one",
                    "L2-107 AC2",
                    settings.BoardMilliseconds),
                () => client.GetAsync("/board", cancellationToken),
                cancellationToken);
        }

        await storm.CancelAsync();

        try
        {
            await Task.WhenAll(heavy);
        }
        catch (OperationCanceledException)
        {
            // Expected: that is how the storm is stopped.
        }

        return [underLoad, beside];
    }

    /// <summary>
    /// Warms up, then times the same call repeatedly and reports the distribution.
    /// </summary>
    /// <remarks>
    /// A failing status is not a slow response, it is a broken harness — measuring the latency of
    /// a 500 would report a very fast API doing nothing.
    /// </remarks>
    private static async Task<Measurement> MeasureAsync(
        Budget budget,
        Func<Task<HttpResponseMessage>> call,
        CancellationToken cancellationToken)
    {
        for (var warmUp = 0; warmUp < WarmUpRequests; warmUp++)
        {
            using var response = await call();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"{budget.Name}: the API answered {(int)response.StatusCode} while warming up. "
                    + await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }

        var samples = new List<double>(SamplesPerEndpoint);

        for (var sample = 0; sample < SamplesPerEndpoint; sample++)
        {
            var started = Stopwatch.GetTimestamp();

            using var response = await call();

            await response.Content.ReadAsByteArrayAsync(cancellationToken);

            samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        samples.Sort();

        return new Measurement(
            budget,
            samples.Count,
            Percentile(samples, 0.50),
            Percentile(samples, 0.95),
            samples[^1]);
    }

    /// <summary>The nearest-rank percentile, which is what "95th percentile" means plainly.</summary>
    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        var rank = (int)Math.Ceiling(percentile * sorted.Count) - 1;

        return sorted[Math.Clamp(rank, 0, sorted.Count - 1)];
    }

    private static HttpClient ClientFor(Settings settings, string accessToken) =>
        new()
        {
            BaseAddress = new Uri(settings.BaseAddress),
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
        };

    private static IAccessTokenIssuer Issuer(Settings settings) =>
        new JwtIssuer(
            Options.Create(new JwtOptions
            {
                Issuer = settings.JwtIssuer,
                Audience = settings.JwtAudience,
                SigningKey = settings.JwtSigningKey,
            }),
            TimeProvider.System);

    /// <summary>Enough of the connection string to recognise, and none of its secrets.</summary>
    private static string Redact(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(
            ';',
            parts.Where(part =>
                part.TrimStart().StartsWith("Server", StringComparison.OrdinalIgnoreCase)
                || part.TrimStart().StartsWith("Database", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Where to measure, and what to measure against.
    /// </summary>
    /// <remarks>
    /// The budgets are settings rather than constants, so a deployment on slower hardware can
    /// state its own and still be honest about it. The defaults are what L2-103 asks for.
    /// </remarks>
    private sealed record Settings(
        string BaseAddress,
        string ConnectionString,
        string JwtIssuer,
        string JwtAudience,
        string JwtSigningKey,
        double BoardMilliseconds,
        double SearchMilliseconds,
        double WriteMilliseconds)
    {
        public static Settings From(string[] args)
        {
            var named = args
                .Where(argument => argument.StartsWith("--", StringComparison.Ordinal) && argument.Contains('=', StringComparison.Ordinal))
                .Select(argument => argument[2..].Split('=', 2))
                .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);

            string Value(string name, string fallback) =>
                named.TryGetValue(name, out var supplied) ? supplied
                : Environment.GetEnvironmentVariable($"BARNABAS_{name.ToUpperInvariant()}") ?? fallback;

            double Number(string name, double fallback) =>
                double.TryParse(Value(name, string.Empty), CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : fallback;

            return new Settings(
                Value("api", "http://localhost:5003"),
                Value(
                    "database",
                    @"Server=.\SQLEXPRESS;Database=Barnabas_E2E;Trusted_Connection=True;TrustServerCertificate=True"),
                Value("jwt-issuer", "barnabas"),
                Value("jwt-audience", "barnabas"),
                Value("jwt-key", "development-only-signing-key-not-for-any-real-deployment"),
                Number("board-budget", 300),
                Number("search-budget", 500),
                Number("write-budget", 500));
        }
    }
}
