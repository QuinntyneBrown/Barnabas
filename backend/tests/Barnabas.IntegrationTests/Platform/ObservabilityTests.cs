using System.Net;
using System.Text.Json;
using Barnabas.Api.Observability;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barnabas.IntegrationTests.Platform;

// Acceptance Test
// Traces to: L2-116, L2-117, L2-118, L2-119
// Description: The API says which dependency is unwell without saying anything about how it is
// configured; every entry it writes carries a correlation identifier, reusing the caller's when
// there is one; it counts, times and classifies what it served; and none of it holds a member's
// address, their words, or a stack trace.
public sealed class ObservabilityTests : AcceptanceTest
{
    public ObservabilityTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-116 AC1: Given a running system with its dependencies available, when health is
    // requested, then the response is 200 and names each dependency as healthy.
    [Fact]
    public async Task Health_names_each_dependency_and_says_it_is_well()
    {
        using var client = Api.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var report = JsonDocument.Parse(await response.ReadBodyAsync());

        report.RootElement.GetProperty("status").GetString().ShouldBe("healthy");

        var dependencies = report.RootElement.GetProperty("dependencies").EnumerateArray().ToList();

        dependencies.ShouldNotBeEmpty();

        var database = dependencies.ShouldHaveSingleItem();

        database.GetProperty("name").GetString().ShouldBe("database");
        database.GetProperty("status").GetString().ShouldBe("healthy");
    }

    // L2-116 AC2: Given an unavailable database, when health is requested, then the response is
    // 503 and names the database as unhealthy.
    [Fact]
    public async Task Health_says_503_when_the_database_is_not_there()
    {
        // Pointed at a server that is not listening, rather than by stopping SQL Express — the
        // check has to fail the way it would in a deployment, which is a connection that does not
        // open.
        using var broken = Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "Database:ConnectionString",
                "Server=127.0.0.1,14330;Database=Barnabas;Trusted_Connection=True;"
                + "TrustServerCertificate=True;Connect Timeout=2");

            // The host applies migrations at start, which against a database that is not there
            // would fail before anything could ask the health endpoint what it thinks.
            builder.UseSetting("Database:MigrateOnStart", "false");
        });

        using var client = broken.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        using var report = JsonDocument.Parse(await response.ReadBodyAsync());

        report.RootElement.GetProperty("status").GetString().ShouldBe("unhealthy");

        report.RootElement.GetProperty("dependencies").EnumerateArray()
            .ShouldContain(dependency =>
                dependency.GetProperty("name").GetString() == "database"
                && dependency.GetProperty("status").GetString() == "unhealthy");
    }

    // L2-116 AC3: Given the health endpoint, when it is requested without credentials, then it
    // returns a status without disclosing connection strings or versions.
    [Fact]
    public async Task Health_answers_a_stranger_without_saying_how_it_is_wired()
    {
        using var stranger = Api.CreateClient();

        var body = await (await stranger.GetAsync("/health", TestContext.Current.CancellationToken))
            .ReadBodyAsync();

        // Everything a connection string is made of, and everything a stack trace is made of.
        foreach (var disclosure in new[]
                 {
                     "Server=",
                     "Data Source",
                     "Trusted_Connection",
                     "Password",
                     "SQLEXPRESS",
                     "Barnabas.Infrastructure",
                     "Microsoft.Data",
                     "at ",
                 })
        {
            body.ShouldNotContain(disclosure, Case.Insensitive, $"health disclosed {disclosure}");
        }
    }

    // L2-117 AC1: Given any request, when it is processed, then a correlation identifier is
    // recorded on every log entry arising from it.
    [Fact]
    public async Task Every_entry_a_request_produces_carries_a_correlation_identifier()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        Api.Logs.Clear();

        await marion.GetAsync("/board", TestContext.Current.CancellationToken);

        var entries = Api.Logs.Entries;

        entries.ShouldNotBeEmpty();

        // Not "some entry mentions one" but "no entry lacks one". The identifier lives in a scope
        // the middleware opened, which is where a real log processor would look for it.
        entries.ShouldAllBe(entry => entry.ScopeValue("CorrelationId") != null);
    }

    // L2-117 AC2: Given a request carrying a correlation identifier, when it is processed, then
    // that identifier is used rather than a new one.
    [Fact]
    public async Task A_supplied_identifier_is_the_one_that_is_used()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        const string Supplied = "supplied-by-the-caller-01";

        Api.Logs.Clear();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/board");

        request.Headers.Add(CorrelationMiddleware.HeaderName, Supplied);

        var response = await marion.SendAsync(request, TestContext.Current.CancellationToken);

        // Echoed back, so a member reporting a fault can be asked for a number that finds it.
        response.Headers.GetValues(CorrelationMiddleware.HeaderName).ShouldContain(Supplied);

        Api.Logs.Entries.ShouldAllBe(entry => entry.ScopeValue("CorrelationId") == Supplied);
    }

    // A header that could break a log line is not reused. One forged newline is how one entry
    // becomes two convincing ones.
    [Fact]
    public async Task An_identifier_that_could_forge_a_log_line_is_replaced()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        Api.Logs.Clear();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/board");

        request.Headers.TryAddWithoutValidation(
            CorrelationMiddleware.HeaderName,
            "real-id\r\nERROR: everything is fine");

        await marion.SendAsync(request, TestContext.Current.CancellationToken);

        Api.Logs.Entries.ShouldAllBe(entry =>
            entry.ScopeValue("CorrelationId") != null
            && !entry.ScopeValue("CorrelationId")!.Contains("everything is fine", StringComparison.Ordinal));
    }

    // L2-117 AC3: Given a handled failure, when it is logged, then the entry carries the
    // correlation identifier, the endpoint, and the congregation identifier.
    [Fact]
    public async Task A_refusal_is_logged_with_the_identifier_the_endpoint_and_the_congregation()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        Api.Logs.Clear();

        // A refusal the product handles rather than a crash: an ordinary member reaching for the
        // moderation queue.
        var response = await priya.GetAsync("/moderation/listings", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var refusal = Api.Logs.Entries
            .Where(entry => entry.Message.Contains("refused", StringComparison.OrdinalIgnoreCase))
            .ShouldHaveSingleItem();

        refusal.ScopeValue("CorrelationId").ShouldNotBeNullOrWhiteSpace();
        refusal.ScopeValue("Endpoint").ShouldBe("GET /moderation/listings");
        refusal.ScopeValue("CongregationId").ShouldBe(SeedData.StAidans.Id.ToString());
    }

    // L2-118 AC1: Given traffic to the API, when metrics are scraped, then request count, error
    // count, and latency distribution are exposed per endpoint.
    [Fact]
    public async Task Metrics_count_time_and_classify_what_was_served()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        for (var call = 0; call < 3; call++)
        {
            await marion.GetAsync("/board", TestContext.Current.CancellationToken);
        }

        var metrics = await ScrapeAsync();

        // By route pattern rather than by path, so every listing is one series instead of one
        // each. A metric with an identifier in its tags grows without bound.
        metrics.ShouldContain("barnabas_requests{endpoint=\"GET /board\",status=\"200\"} 3");

        // The distribution, as a count and a sum - which is what gives a mean per endpoint.
        metrics.ShouldContain("barnabas_duration_count{endpoint=\"GET /board\"} 3");
        metrics.ShouldContain("barnabas_duration_sum{endpoint=\"GET /board\"}");

        // And no series for a request that never happened.
        metrics.ShouldNotContain("barnabas_errors{endpoint=\"GET /board\"}");
    }

    // L2-118 AC2: Given a rate-limited request, when metrics are scraped, then the rejection is
    // counted.
    [Fact]
    public async Task A_rate_limited_request_is_counted_as_a_rejection()
    {
        using var stranger = Api.CreateClient();

        // The sign-in link throttle allows five in fifteen minutes; the sixth is refused.
        HttpStatusCode last = HttpStatusCode.OK;

        for (var attempt = 0; attempt < 7; attempt++)
        {
            var response = await stranger.PostJsonAsync(
                "/sessions/link",
                new { emailAddress = SeedData.Marion.EmailAddress },
                TestContext.Current.CancellationToken);

            last = response.StatusCode;
        }

        last.ShouldBe(HttpStatusCode.TooManyRequests);

        var metrics = await ScrapeAsync();

        metrics.ShouldContain("barnabas_rejections{endpoint=\"POST /sessions/link\"}");

        // Counted as a rejection and not as an error. A rate limit doing its job is the product
        // working, and folding it into the error rate would make that look like a fault.
        metrics.ShouldNotContain("barnabas_errors{endpoint=\"POST /sessions/link\"}");
    }

    // L2-119 AC1: Given any log entry, when it is inspected, then it contains no email address, no
    // message body, and no listing description.
    [Fact]
    public async Task Nothing_a_member_wrote_reaches_the_log()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        const string Description = "A description nobody should find in a log file.";
        const string MessageBody = "A message nobody should find in a log file.";

        Api.Logs.Clear();

        var posted = await priya.PostJsonAsync(
            "/listings/give",
            new
            {
                title = "Two folding chairs",
                description = Description,
                category = "Furniture",
                neighbourhood = SeedData.Priya.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        posted.StatusCode.ShouldBe(HttpStatusCode.Created);

        var made = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        var accepted = await marion.PostJsonAsync(
            $"/requests/{(await made.ReadAsync<MadeRequest>()).RequestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        await priya.PostJsonAsync(
            $"/threads/{(await accepted.ReadAsync<AcceptedRequest>()).ThreadId}/messages",
            new { body = MessageBody },
            TestContext.Current.CancellationToken);

        // Sign-in, so an address has been through the API on this run.
        await priya.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = SeedData.Grace.EmailAddress },
            TestContext.Current.CancellationToken);

        var everything = Api.Logs.Everything;

        everything.ShouldNotContain(Description);
        everything.ShouldNotContain(MessageBody);
        everything.ShouldNotContain(SeedData.Grace.EmailAddress, Case.Insensitive);
        everything.ShouldNotContain("@example.com", Case.Insensitive);
    }

    // L2-119 AC2: Given a validation failure, when it is logged, then the offending value is not
    // recorded, only the field name.
    [Fact]
    public async Task A_validation_failure_records_the_field_and_not_the_value()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        const string Offending = "an-offending-value-nobody-should-log";

        Api.Logs.Clear();

        var response = await priya.PostJsonAsync(
            "/listings/give",
            new
            {
                title = string.Empty,
                description = Offending,
                category = "Furniture",
                neighbourhood = SeedData.Priya.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Api.Logs.Everything.ShouldNotContain(Offending);
    }

    // L2-119 AC3: Given an error surfaced to a client, when it is inspected, then it contains no
    // stack trace and no internal identifier.
    [Fact]
    public async Task A_refusal_a_client_sees_carries_no_stack_trace_and_no_internals()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        foreach (var (route, method) in new[]
                 {
                     ("/moderation/listings", HttpMethod.Get),
                     ($"/listings/{Guid.NewGuid()}", HttpMethod.Get),
                     ("/listings/give", HttpMethod.Post),
                 })
        {
            using var request = new HttpRequestMessage(method, route)
            {
                Content = method == HttpMethod.Post ? Json.From.Of(new { title = string.Empty }) : null,
            };

            var body = await (await priya.SendAsync(request, TestContext.Current.CancellationToken))
                .ReadBodyAsync();

            foreach (var internals in new[]
                     {
                         "Barnabas.Application",
                         "Barnabas.Infrastructure",
                         "System.",
                         "   at ",
                         "Exception",
                         "StackTrace",
                     })
            {
                body.ShouldNotContain(internals, Case.Insensitive, $"{route} disclosed {internals}");
            }
        }
    }

    private async Task<string> ScrapeAsync()
    {
        using var scraper = Api.CreateClient();

        var response = await scraper.GetAsync("/metrics", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadBodyAsync();
    }
}
