using System.Collections.Concurrent;
using Barnabas.Application.Common.Abuse;

namespace Barnabas.Infrastructure.Security;

/// <inheritdoc />
/// <remarks>
/// The same sliding window the sign-in throttle keeps, counted against the injected clock for the
/// same reason: a test can move past the window rather than wait out an hour.
/// </remarks>
public sealed class RedemptionThrottle : IRedemptionThrottle
{
    /// <summary>Ten wrong guesses in an hour is not somebody mistyping a code off a card.</summary>
    public const int Limit = 10;

    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _failures = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public RedemptionThrottle(TimeProvider time) => _time = time;

    public bool MayTry(string source)
    {
        if (!_failures.TryGetValue(source, out var failures))
        {
            return true;
        }

        var since = _time.GetUtcNow() - Window;

        lock (failures)
        {
            failures.RemoveAll(failure => failure <= since);

            return failures.Count < Limit;
        }
    }

    public void RecordFailure(string source)
    {
        var failures = _failures.GetOrAdd(source, _ => []);

        lock (failures)
        {
            failures.Add(_time.GetUtcNow());
        }
    }

    /// <summary>Forgets every source, so one acceptance test does not block the next.</summary>
    public void Clear() => _failures.Clear();
}
