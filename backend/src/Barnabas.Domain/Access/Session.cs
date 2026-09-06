using Barnabas.Domain.Common;

namespace Barnabas.Domain.Access;

/// <summary>
/// One member's sign-in on one device, holding the access and refresh tokens issued
/// together and revocable independently of that member's other sign-ins.
/// </summary>
/// <remarks>
/// Signing out revokes the session rather than a token, which matters twice over.
/// L2-019 admits no window between signing out and tokens being refused, and a signature
/// cannot express revocation — so <see cref="IsLive"/> is read on every authenticated
/// request. And a member signed in on a phone and a laptop holds two sessions, so signing
/// out on one leaves the other working.
/// </remarks>
public sealed class Session : ITenantOwned
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private Session()
    {
    }

    public Session(Guid id, Guid congregationId, Guid memberId, DateTimeOffset openedAt)
    {
        Id = id;
        CongregationId = congregationId;
        MemberId = memberId;
        OpenedAt = openedAt;
        ExpiresAt = openedAt + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid MemberId { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Whether tokens carrying this session's identifier are still accepted.
    /// </summary>
    public bool IsLive(DateTimeOffset asOf) => !IsRevoked && asOf < ExpiresAt;

    public void Revoke(DateTimeOffset asOf) => RevokedAt ??= asOf;
}
