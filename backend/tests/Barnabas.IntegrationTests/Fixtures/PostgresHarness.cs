using Barnabas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Barnabas.IntegrationTests.Fixtures;

/// <inheritdoc />
public sealed class PostgresHarness : DatabaseHarness
{
    private PostgreSqlContainer? _container;

    public override string Provider => DatabaseProviders.Postgres;

    public override string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("The PostgreSQL container has not been started.");

    /// <inheritdoc />
    /// <remarks>
    /// The container is built here rather than in a field, because building one resolves the
    /// Docker endpoint - so a machine with no container runtime fails during construction, where
    /// the guidance below could not be attached to it.
    /// </remarks>
    public override async ValueTask StartAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("barnabas")
                .WithUsername("barnabas")
                .WithPassword("barnabas")
                .Build();

            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            // The failure a developer actually hits is a container runtime that is not running,
            // and the raw error for that is a wall of socket detail.
            throw new InvalidOperationException(
                "Could not start the PostgreSQL test container. Start Docker Desktop, or run the "
                + "suite against SQLite with BARNABAS_TEST_DB=sqlite - note that the concurrency "
                + "tests report as skipped on SQLite, because it cannot express a row version.",
                exception);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
