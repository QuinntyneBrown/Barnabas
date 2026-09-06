using Barnabas.Domain.Access;
using Barnabas.Domain.Members;

namespace Barnabas.Application.Common.Persistence;

/// <summary>
/// The one sanctioned way to read before a congregation is known.
/// </summary>
/// <remarks>
/// Signing in is anonymous: a member is found by email address, and only the resulting
/// record says which congregation they belong to. A filter keyed on a token that does not
/// yet exist cannot serve that lookup.
/// <para>
/// This store is deliberately narrow, and keeping it narrow is what stops the bypass
/// widening: there is no method on it that returns a listing, a request, or a thread. It
/// returns authentication records and nothing else.
/// </para>
/// </remarks>
public interface IAuthenticationStore
{
    /// <summary>Finds an approved member by email address, across all congregations.</summary>
    Task<Member?> FindSignableMemberByEmailAsync(string emailAddress, CancellationToken cancellationToken);

    Task AddSignInTokenAsync(SignInToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Consumes a sign-in token if it is still redeemable, and reports whether this caller
    /// was the one that consumed it.
    /// </summary>
    /// <remarks>
    /// A conditional update whose affected row count decides the winner, not a read
    /// followed by a save. L2-016 requires exactly one session from a concurrent exchange,
    /// and a read-then-write cannot promise that: two callers can both read an unconsumed
    /// token and both proceed. The loser receives 410, the same answer a genuinely reused
    /// link gets.
    /// </remarks>
    Task<SignInToken?> TryConsumeSignInTokenAsync(
        string tokenHash,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task<Member?> FindMemberByIdAsync(Guid memberId, CancellationToken cancellationToken);

    Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task AddSessionAsync(Session session, CancellationToken cancellationToken);

    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token if it is still active, and reports whether this caller won.
    /// </summary>
    /// <remarks>
    /// The same conditional-update reasoning as
    /// <see cref="TryConsumeSignInTokenAsync"/>. Two callers racing produce one winner and
    /// one 401 rather than two valid tokens, which is what L2-018 requires.
    /// </remarks>
    Task<bool> TryRotateRefreshTokenAsync(
        Guid tokenId,
        Guid replacementTokenId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

    /// <summary>Revokes a session and the refresh tokens bound to it.</summary>
    Task RevokeSessionAsync(Guid sessionId, DateTimeOffset asOf, CancellationToken cancellationToken);
}
