using Barnabas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// The database the acceptance suite runs against.
/// </summary>
/// <remarks>
/// PostgreSQL is the real answer, and the one the concurrency rules are written against: the
/// filtered unique index and the <c>xmin</c> row version are what make single-use links,
/// one-decision-per-request, and one-open-request-per-member true under a race, and no code
/// check can stand in for them.
/// <para>
/// SQLite is here so the suite still runs on a machine with no container runtime. It cannot
/// express a row version, so the handful of tests that turn on one declare that they need
/// PostgreSQL and report as skipped rather than passing vacuously.
/// </para>
/// </remarks>
public abstract class DatabaseHarness : IAsyncDisposable
{
    private const string ProviderVariable = "BARNABAS_TEST_DB";

    public abstract string Provider { get; }

    public abstract string ConnectionString { get; }

    public bool SupportsRowVersions => Provider == DatabaseProviders.Postgres;

    public static DatabaseHarness Create() =>
        string.Equals(
            Environment.GetEnvironmentVariable(ProviderVariable),
            DatabaseProviders.Sqlite,
            StringComparison.OrdinalIgnoreCase)
            ? new SqliteHarness()
            : new PostgresHarness();

    public abstract ValueTask StartAsync();

    public abstract ValueTask DisposeAsync();
}
