using Barnabas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Barnabas.IntegrationTests.Fixtures;

/// <inheritdoc />
public sealed class PostgresHarness : DatabaseHarness
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("barnabas")
        .WithUsername("barnabas")
        .WithPassword("barnabas")
        .Build();

    public override string Provider => DatabaseProviders.Postgres;

    public override string ConnectionString => _container.GetConnectionString();

    public override async ValueTask StartAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            // The failure a developer actually hits is a container runtime that is not running,
            // and the raw error for that is a wall of socket detail. Say what to do instead.
            throw new InvalidOperationException(
                "Could not start the PostgreSQL test container. Start Docker Desktop, or run the "
                + "suite against SQLite with BARNABAS_TEST_DB=sqlite - note that the concurrency "
                + "tests report as skipped on SQLite because it cannot express a row version.",
                exception);
        }
    }

    public override async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
