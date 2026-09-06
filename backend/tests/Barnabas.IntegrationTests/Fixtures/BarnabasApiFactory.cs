using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using Barnabas.Domain.Members;
using Barnabas.Infrastructure.Email;
using Barnabas.Infrastructure.Persistence;
using Barnabas.Infrastructure.Security;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// The API under test, wired to a real database and a clock the tests can move.
/// </summary>
/// <remarks>
/// The suite drives the running API over HTTP rather than calling handlers, because most of
/// what feature slice 1 has to prove lives between the two: authentication, the congregation
/// filter, the ownership behaviour, and the error contract are all pipeline, and a test that
/// calls a handler directly would assert none of them.
/// </remarks>
public sealed class BarnabasApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Shared with the forged tokens, so "correctly signed" means the same thing to both.</summary>
    public const string SigningKey = "integration-tests-signing-key-not-for-any-real-deployment";

    /// <summary>
    /// The same list the development reset uses.
    /// </summary>
    /// <remarks>
    /// Shared rather than copied. This was two lists, and they drifted: tables added for joining,
    /// help tags, availability windows and notifications were emptied here and left standing in
    /// the Playwright suite, where leaked state reads as a product defect.
    /// </remarks>
    private static readonly string[] TablesInDeletionOrder = DatabaseReset.TablesInDeletionOrder;

    private readonly TestDatabase _database = new();

    /// <summary>
    /// A clock the tests move deliberately. Expiry is a rule about elapsed time, and a test that
    /// waited for it would be slow and flaky in equal measure.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 5, 9, 0, 0, TimeSpan.Zero));

    public IEmailOutbox Outbox => Services.GetRequiredService<IEmailOutbox>();

    /// <summary>Whatever the API logged as an error during this test, ready to be reported.</summary>
    public ServerErrorLog ServerErrors { get; } = new();

    public async ValueTask InitializeAsync()
    {
        // Touching the client forces the host to build, which applies the migration.
        using var client = CreateClient();

        await ResetAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        await _database.DisposeAsync();
    }

    /// <summary>Empties every table and re-seeds, so each test starts from the same board.</summary>
    public async Task ResetAsync()
    {
        // The throttle counts in memory across requests, so one test's five attempts would
        // otherwise be the next test's head start.
        Services.GetRequiredService<SignInLinkThrottle>().Clear();
        Services.GetRequiredService<RedemptionThrottle>().Clear();

        // The clock is not rewound between tests. A fake clock refuses to go backwards, and
        // nothing here needs it to: every assertion about time is relative to when the test
        // itself created the row it is reasoning about.
        Outbox.Clear();
        ServerErrors.Clear();

        await using var scope = Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<BarnabasDbContext>();

        foreach (var table in TablesInDeletionOrder)
        {
            // The table names are a fixed list in this file, not anything a test supplies.
#pragma warning disable EF1002
            await context.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"");
#pragma warning restore EF1002
        }

        await scope.ServiceProvider.GetRequiredService<CongregationSeeder>().SeedAsync();
    }

    /// <summary>
    /// Opens a real session for a seeded member and returns the access token for it.
    /// </summary>
    /// <remarks>
    /// A real <see cref="Session"/> row rather than a hand-forged token, because every
    /// authenticated request reads the session to decide whether it still stands. A token
    /// naming a session that was never opened is refused, which is the point of that read.
    /// </remarks>
    public async Task<string> SignInAsync(Guid memberId)
    {
        await using var scope = Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<BarnabasDbContext>();

        var member = await context.Set<Member>()
            .IgnoreQueryFilters()
            .SingleAsync(m => m.Id == memberId);

        var session = new Session(Guid.NewGuid(), member.CongregationId, member.Id, Clock.GetUtcNow());

        context.Add(session);

        await context.SaveChangesAsync();

        return scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>().Issue(member, session).Value;
    }

    public async Task<HttpClient> ClientForAsync(Guid memberId)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await SignInAsync(memberId));

        return client;
    }

    /// <summary>
    /// Writes directly, to arrange state a test is not itself about.
    /// </summary>
    /// <remarks>
    /// Used sparingly. A test for what the board shows should go through the posting endpoint,
    /// because that is the path a member takes; a test for how my-listings counts open requests
    /// should not also be a test of the request endpoint.
    /// </remarks>
    public async Task ArrangeAsync(Func<BarnabasDbContext, Task> arrange)
    {
        ArgumentNullException.ThrowIfNull(arrange);

        await using var scope = Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<BarnabasDbContext>();

        await arrange(context);

        await context.SaveChangesAsync();
    }

    /// <summary>Reads the database directly, for assertions the API deliberately does not expose.</summary>
    public async Task<T> QueryAsync<T>(Func<BarnabasDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var scope = Services.CreateAsyncScope();

        return await query(scope.ServiceProvider.GetRequiredService<BarnabasDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.UseSetting("Database:ConnectionString", _database.ConnectionString);
        builder.UseSetting("Database:Seed", "false");
        builder.UseSetting("Database:ResetOnStart", "false");
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Auth:RefreshCookie:Secure", "false");

        builder.ConfigureLogging(logging => logging.AddProvider(ServerErrors));

        builder.ConfigureServices(services => services.Replace(
            ServiceDescriptor.Singleton<TimeProvider>(Clock)));
    }
}
