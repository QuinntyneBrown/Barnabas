using Barnabas.Domain.Common;

namespace Barnabas.Domain.Access;

/// <summary>
/// A single-use secret, valid for a short interval, that a member exchanges for a session.
/// </summary>
/// <remarks>
/// Only the hash is stored. A database disclosure therefore yields nothing usable: the
/// token itself exists only in the email and in the URL the member follows.
/// <para>
/// Expiry is a question this entity can answer. Single use is not — two callers can both
/// read an unconsumed token and both proceed — so <see cref="Consume"/> is the in-memory
/// half of a rule the database settles with a conditional update. See
/// <c>IAuthenticationStore.TryConsumeSignInToken</c>.
/// </para>
/// </remarks>
public sealed class SignInToken : ITenantOwned
{
    /// <summary>
    /// L2-015: a sign-in token shall expire no more than 15 minutes after issue.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    private SignInToken()
    {
    }

    public SignInToken(Guid id, Guid congregationId, Guid memberId, string tokenHash, DateTimeOffset issuedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        CongregationId = congregationId;
        MemberId = memberId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = issuedAt + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid MemberId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt is not null;

    public bool HasExpired(DateTimeOffset asOf) => asOf >= ExpiresAt;

    public bool IsRedeemable(DateTimeOffset asOf) => !IsConsumed && !HasExpired(asOf);

    public void Consume(DateTimeOffset asOf)
    {
        if (!IsRedeemable(asOf))
        {
            throw new SignInTokenNotRedeemableException();
        }

        ConsumedAt = asOf;
    }
}
