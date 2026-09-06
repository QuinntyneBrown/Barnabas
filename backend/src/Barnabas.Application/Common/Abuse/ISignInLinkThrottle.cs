namespace Barnabas.Application.Common.Abuse;

/// <summary>
/// Caps how often a sign-in link may be asked for on one address.
/// </summary>
/// <remarks>
/// Application state rather than the framework's rate limiter, and deliberately so.
/// <c>L2-017 AC2</c> requires a further request to succeed once fifteen minutes have elapsed, and
/// the middleware's window is replenished by an internal timer that the injected
/// <see cref="TimeProvider"/> cannot advance — so the criterion could only be tested by waiting a
/// real quarter of an hour. Counted against the clock the rest of the system already uses, it is
/// deterministic.
/// <para>
/// The address rather than the source, because what this protects is a mailbox: somebody else's
/// inbox filling up is the harm, and a limiter keyed on the sender's network would not touch it.
/// The per-source cap is the framework's job and is applied alongside — <c>L2-099</c>.
/// </para>
/// </remarks>
public interface ISignInLinkThrottle
{
    /// <summary>Whether another link may be sent to this address now, counting this attempt.</summary>
    bool TryRecord(string emailAddress);
}
