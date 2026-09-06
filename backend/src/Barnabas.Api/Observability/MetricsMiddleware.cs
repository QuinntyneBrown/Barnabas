using System.Diagnostics;

namespace Barnabas.Api.Observability;

/// <summary>
/// Times every request and records what became of it.
/// </summary>
/// <remarks>
/// Outside the exception handler, so a request that failed is still counted and still timed with
/// the status the caller was actually given. A middleware that only saw successes would make the
/// error rate zero by construction.
/// <para>
/// The route is read on the way in rather than on the way out, because the exception handler
/// clears the endpoint when it handles something - and a refusal recorded as <c>unmatched</c>
/// would put every 429 and every 403 in the product into one series with no route on it.
/// </para>
/// </remarks>
public sealed class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BarnabasMetrics _metrics;

    public MetricsMiddleware(RequestDelegate next, BarnabasMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var endpoint = EndpointOf(context);
        var started = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            _metrics.Record(
                endpoint,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    /// <summary>
    /// The route pattern, or the method alone when nothing matched.
    /// </summary>
    /// <remarks>
    /// The pattern rather than the path, so every listing is one series instead of one each. A
    /// request that matched no endpoint is recorded as <c>unmatched</c> rather than by its path,
    /// for the same reason: a scanner walking a thousand URLs would otherwise create a thousand
    /// series.
    /// </remarks>
    private static string EndpointOf(HttpContext context)
    {
        var pattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

        return pattern is null
            ? $"{context.Request.Method} unmatched"
            : $"{context.Request.Method} /{pattern.TrimStart('/')}";
    }
}
