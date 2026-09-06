using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;

namespace Barnabas.Api.Observability;

/// <summary>
/// Listens to the meter and holds what it has heard, so it can be read back on request.
/// </summary>
/// <remarks>
/// A <see cref="MeterListener"/> rather than an exporter package. Barnabas records into the
/// platform's own instruments; a deployment that wants OpenTelemetry adds the exporter and points
/// it at the same meter, and nothing here changes. This exists so that <c>L2-118</c>'s "when
/// metrics are scraped" is answerable without the product having chosen a monitoring vendor.
/// <para>
/// The format is Prometheus text exposition, because that is the one every collector reads and
/// because a shape nobody recognises would be a metrics endpoint in name only.
/// </para>
/// <para>
/// The histogram is kept as a count and a sum rather than as buckets. What <c>L2-118 AC1</c> asks
/// for is a latency distribution per endpoint; count and sum give the mean, and a collector that
/// wants quantiles wants a real backend rather than an in-process ring buffer.
/// </para>
/// </remarks>
public sealed class MetricsSnapshot : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, long> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (long Count, double Sum)> _histograms = new(StringComparer.Ordinal);

    private readonly MeterListener _listener;

    public MetricsSnapshot()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BarnabasMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        _listener.SetMeasurementEventCallback<long>(OnCount);
        _listener.SetMeasurementEventCallback<double>(OnDuration);
        _listener.Start();
    }

    /// <summary>Every series, in Prometheus text exposition format.</summary>
    public string Render()
    {
        var text = new StringBuilder();

        lock (_gate)
        {
            foreach (var (series, value) in _counters.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                text.Append(series).Append(' ')
                    .Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            foreach (var (series, measured) in _histograms.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                var (name, labels) = Split(series);

                text.Append(name).Append("_count").Append(labels).Append(' ')
                    .Append(measured.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');

                text.Append(name).Append("_sum").Append(labels).Append(' ')
                    .Append(measured.Sum.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>Forgets everything, so one acceptance test's traffic is not another's.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _counters.Clear();
            _histograms.Clear();
        }
    }

    public void Dispose() => _listener.Dispose();

    private void OnCount(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        var series = Series(instrument.Name, tags);

        lock (_gate)
        {
            _counters[series] = _counters.GetValueOrDefault(series) + measurement;
        }
    }

    private void OnDuration(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        var series = Series(instrument.Name, tags);

        lock (_gate)
        {
            var (count, sum) = _histograms.GetValueOrDefault(series);

            _histograms[series] = (count + 1, sum + measurement);
        }
    }

    /// <summary>The instrument and its tags, rendered as one Prometheus series name.</summary>
    private static string Series(string instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var name = instrument.Replace('.', '_');

        if (tags.Length == 0)
        {
            return name;
        }

        var labels = new List<string>(tags.Length);

        foreach (var tag in tags)
        {
            labels.Add($"{tag.Key}=\"{Escape(tag.Value?.ToString() ?? string.Empty)}\"");
        }

        return $"{name}{{{string.Join(",", labels)}}}";
    }

    private static (string Name, string Labels) Split(string series)
    {
        var brace = series.IndexOf('{', StringComparison.Ordinal);

        return brace < 0 ? (series, string.Empty) : (series[..brace], series[brace..]);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
