using Barnabas.Domain.Common;

namespace Barnabas.Domain.Congregations;

/// <summary>
/// The entitlement to finish joining, held between redeeming a code and supplying a profile.
/// </summary>
/// <remarks>
/// Joining is two steps and cannot be collapsed into one. <c>L2-006</c> says redeeming begins the
/// flow and returns the congregation; <c>L2-009</c> says the code is spent exactly once, including
/// under concurrency; and <c>L2-010</c> says a bad profile creates no member. So the code is burnt
/// at step one and something has to carry the right to step two.
/// <para>
/// A hashed bearer token rather than the congregation's identifier, for the same reason a sign-in
/// link is: handing back an identifier would let the next call name any congregation it liked.
/// Only the hash is stored, so a database disclosure yields nothing usable.
/// </para>
/// <para>
/// The consequence, recorded in ADR-0002: abandoning the profile step burns the code. Fifteen
/// minutes, matching the sign-in token, because both are single-use credentials held by somebody
/// the system does not yet know.
/// </para>
/// </remarks>
public sealed class JoiningSession : ITenantOwned
{
    private JoiningSession()
    {
    }

    public JoiningSession(
        Guid id,
        Guid congregationId,
        Guid inviteCodeId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        CongregationId = congregationId;
        InviteCodeId = inviteCodeId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid InviteCodeId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsUsable(DateTimeOffset asOf) => ConsumedAt is null && asOf < ExpiresAt;

    /// <summary>
    /// Records that it was spent.
    /// </summary>
    /// <remarks>
    /// As with the code, single use is decided by a conditional update rather than here; two
    /// profiles submitted against one capability both find it usable before either commits.
    /// </remarks>
    public void Consume(DateTimeOffset asOf) => ConsumedAt = asOf;
}
