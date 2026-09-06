using Barnabas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Barnabas.Api.Observability;

/// <summary>
/// Asks the database whether it is there.
/// </summary>
/// <remarks>
/// <c>CanConnectAsync</c> rather than a query against a table. A check that read rows would fail
/// while a migration held a lock, and reporting an outage during a deployment is how a health
/// check teaches people to ignore it.
/// <para>
/// The failure carries no description. What went wrong is in the log with its correlation
/// identifier; what the endpoint says is that the database is unhealthy, because
/// <c>L2-116 AC3</c> refuses to disclose a connection string to an anonymous caller and a
/// connection failure's message is the connection string.
/// </para>
/// </remarks>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly BarnabasDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(BarnabasDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception failure)
        {
            _logger.LogError(failure, "The database health check could not reach the database.");

            return HealthCheckResult.Unhealthy();
        }
    }
}
