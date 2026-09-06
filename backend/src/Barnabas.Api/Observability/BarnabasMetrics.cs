using System.Diagnostics.Metrics;

namespace Barnabas.Api.Observability;

/// <summary>
/// The instruments Barnabas records, and the names they go by.
/// </summary>
/// <remarks>
/// <see cref="System.Diagnostics.Metrics"/> rather than a library, because it is the platform's
/// own vocabulary: anything that can read a <see cref="MeterListener"/> — an OpenTelemetry
/// exporter, the dotnet-counters tool, or the endpoint in this folder — can read these without
/// Barnabas having chosen a monitoring product on a deployment's behalf.
/// <para>
/// Three instruments, which are the three <c>L2-118</c> asks for: how many, how many went wrong,
/// and how long. Every one is tagged with the endpoint rather than named after it, because a
/// metric per route is a cardinality problem and a tag is what a query groups by.
/// </para>
/// </remarks>
public sealed class BarnabasMetrics
{
    public const string MeterName = "Barnabas.Api";

    private readonly Counter<long> _requests;
    private readonly Counter<long> _errors;
    private readonly Counter<long> _rejections;
    private readonly Histogram<double> _duration;

    public BarnabasMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);

        _requests = meter.CreateCounter<long>(
            "barnabas.requests",
            unit: "{request}",
            description: "Requests handled, by endpoint and status.");

        _errors = meter.CreateCounter<long>(
            "barnabas.errors",
            unit: "{request}",
            description: "Requests answered with 5xx, by endpoint.");

        _rejections = meter.CreateCounter<long>(
            "barnabas.rejections",
            unit: "{request}",
            description: "Requests refused by a rate limit, by endpoint.");

        _duration = meter.CreateHistogram<double>(
            "barnabas.duration",
            unit: "ms",
            description: "How long a request took, by endpoint.");
    }

    /// <summary>
    /// Records one finished request.
    /// </summary>
    /// <remarks>
    /// The route pattern is used rather than the path, so <c>/listings/{listingId}</c> is one
    /// series rather than one per listing. A metric with an identifier in its tags is a metric
    /// that grows without bound and is thrown away by whatever is storing it.
    /// </remarks>
    public void Record(string endpoint, int statusCode, double elapsedMilliseconds)
    {
        var byEndpoint = new KeyValuePair<string, object?>("endpoint", endpoint);
        var byStatus = new KeyValuePair<string, object?>("status", statusCode);

        _requests.Add(1, byEndpoint, byStatus);
        _duration.Record(elapsedMilliseconds, byEndpoint);

        if (statusCode >= 500)
        {
            _errors.Add(1, byEndpoint);
        }

        // Counted separately from errors. A 429 is the product working, and folding it into the
        // error count would make a rate limit doing its job look like a fault - L2-118 AC2.
        if (statusCode == StatusCodes.Status429TooManyRequests)
        {
            _rejections.Add(1, byEndpoint);
        }
    }
}
