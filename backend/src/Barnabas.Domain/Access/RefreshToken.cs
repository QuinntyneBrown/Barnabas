using Barnabas.Domain.Common;

namespace Barnabas.Domain.Access;

/// <summary>
/// A long-lived secret held by the client that exchanges for a fresh access token, and
/// which is replaced each time it is used.
/// </summary>
/// <remarks>
/// Rotation on use is what makes the pair safe: a refresh token is replaced every time it
/// is exchanged, so presenting an old one is evidence of a copy and is refused.
/// <see cref="ReplacedByTokenId"/> is what makes a superseded token distinguishable from
/// one that never existed.
/// <para>
/// As with sign-in tokens, only the hash is stored, and as with them the race is settled
/// by a conditional update rather than by <see cref="Rotate"/> alone.
/// </para>
/// </remarks>
public sealed class RefreshToken : ITenantOwned
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid id,
        Guid congregationId,
        Guid sessionId,
        string tokenHash,
        DateTimeOffset issuedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        CongregationId = congregationId;
        SessionId = sessionId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = issuedAt + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid SessionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsRotated => ReplacedByTokenId is not null;

    public bool IsActive(DateTimeOffset asOf) => !IsRevoked && !IsRotated && asOf < ExpiresAt;

    public void Rotate(Guid replacementTokenId, DateTimeOffset asOf)
    {
        if (!IsActive(asOf))
        {
            throw new RefreshTokenNotActiveException();
        }

        ReplacedByTokenId = replacementTokenId;
        RevokedAt = asOf;
    }

    public void Revoke(DateTimeOffset asOf) => RevokedAt ??= asOf;
}
