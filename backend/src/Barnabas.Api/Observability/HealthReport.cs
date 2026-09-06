using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Barnabas.Api.Observability;

/// <summary>
/// What the health endpoint says, and — more to the point — what it does not.
/// </summary>
/// <remarks>
/// The default writer returns nothing at all; the obvious alternative returns each check's
/// <c>Exception</c> and <c>Description</c>, which for a database check is the connection string
/// and the server version. <c>L2-116 AC3</c> refuses both, so this writes the two things an
/// operator needs — which dependency, and whether it is up — and nothing else.
/// <para>
/// Anonymous, because a monitor cannot sign in. That is why it is worth being careful about what
/// it says.
/// </para>
/// </remarks>
public static class HealthReport
{
    public static Task WriteAsync(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = Describe(report.Status),
            dependencies = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = Describe(entry.Value.Status),
            }),
        }));
    }

    public static HealthCheckOptions Options() => new()
    {
        ResponseWriter = WriteAsync,

        // Degraded answers 200. A dependency that is slow but working is not an outage, and a
        // load balancer taking the node out of rotation for it would turn a slowdown into one.
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        },
    };

    private static string Describe(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "healthy",
        HealthStatus.Degraded => "degraded",
        _ => "unhealthy",
    };
}
