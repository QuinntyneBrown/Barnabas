using System.Collections.Concurrent;
using Barnabas.Application.Common.Abuse;
using Barnabas.Domain.Members;

namespace Barnabas.Infrastructure.Security;

/// <inheritdoc />
/// <remarks>
/// A sliding window in memory. One process holds the count, which is the right scope for what this
/// defends: a deployment behind several hosts would let a determined sender through at n times the
/// rate, and the harm — a mailbox filling up — is bounded by the mail provider long before that
/// matters. A shared counter would buy little and cost a round trip on every sign-in.
/// </remarks>
public sealed class SignInLinkThrottle : ISignInLinkThrottle
{
    /// <summary>Five in a quarter of an hour, per ADR-0002 and <c>L2-017</c>.</summary>
    public const int Limit = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public SignInLinkThrottle(TimeProvider time) => _time = time;

    public bool TryRecord(string emailAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var address = Member.Normalise(emailAddress);
        var now = _time.GetUtcNow();
        var since = now - Window;

        var attempts = _attempts.GetOrAdd(address, _ => []);

        lock (attempts)
        {
            // Anything older than the window has stopped counting, and dropping it here is what
            // keeps the list from growing for an address somebody keeps trying all week.
            attempts.RemoveAll(attempt => attempt <= since);

            if (attempts.Count >= Limit)
            {
                return false;
            }

            attempts.Add(now);

            return true;
        }
    }

    /// <summary>Forgets every address, so one acceptance test does not throttle the next.</summary>
    public void Clear() => _attempts.Clear();
}
