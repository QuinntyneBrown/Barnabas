using Barnabas.Domain.Common;

namespace Barnabas.Domain.Congregations;

/// <summary>
/// A code a prospective member redeems to join a congregation.
/// </summary>
/// <remarks>
/// Seeded rather than issued through the product in feature slice 1. Redemption is
/// L1-002 and is not built here; the entity exists so the seeded congregation is
/// complete and so the invite-redemption endpoint has something to redeem against
/// when that slice arrives.
/// </remarks>
public sealed class InviteCode : ITenantOwned
{
    private InviteCode()
    {
    }

    public InviteCode(Guid id, Guid congregationId, string code, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Id = id;
        CongregationId = congregationId;
        Code = code.Trim().ToUpperInvariant();
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RedeemedAt { get; private set; }

    public bool IsRedeemable(DateTimeOffset asOf) => RedeemedAt is null && asOf < ExpiresAt;
}
