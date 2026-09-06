using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Access;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// The one sanctioned read before a congregation is known.
/// </summary>
/// <remarks>
/// Every query here ignores the global filter, which is the whole point of the type and the
/// reason it is kept this narrow: signing in is anonymous, so a filter keyed on a token that
/// does not yet exist cannot serve the lookup. There is no method here that returns a listing,
/// a request, or a thread, and adding one would widen a deliberate hole.
/// </remarks>
public sealed class AuthenticationStore : IAuthenticationStore
{
    private readonly BarnabasDbContext _context;

    public AuthenticationStore(BarnabasDbContext context) => _context = context;

    public Task<Member?> FindApprovedMemberByEmailAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var normalised = Member.Normalise(emailAddress);

        return _context.Set<Member>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.EmailAddress == normalised && m.Status == MemberStatus.Approved,
                cancellationToken);
    }

    public async Task AddSignInTokenAsync(SignInToken token, CancellationToken cancellationToken)
    {
        _context.Add(token);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// One conditional update, and the affected-row count decides the winner. A read followed
    /// by a save cannot promise what L2-016 asks for: two callers can both read an unconsumed
    /// token and both proceed. Here the database refuses the second write, and that caller
    /// receives the same 410 a genuinely reused link gets.
    /// </remarks>
    public async Task<SignInToken?> TryConsumeSignInTokenAsync(
        string tokenHash,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var consumed = await _context.Set<SignInToken>()
            .IgnoreQueryFilters()
            .Where(t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > asOf)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ConsumedAt, asOf), cancellationToken);

        if (consumed == 0)
        {
            return null;
        }

        return await _context.Set<SignInToken>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public Task<Member?> FindMemberByIdAsync(Guid memberId, CancellationToken cancellationToken) =>
        _context.Set<Member>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);

    public Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        _context.Set<Session>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task AddSessionAsync(Session session, CancellationToken cancellationToken)
    {
        _context.Add(session);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.Set<RefreshToken>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The same reasoning as the sign-in token consumption. Two callers racing one refresh
    /// token produce one winner and one 401, never two valid tokens, which is what L2-018
    /// requires.
    /// </remarks>
    public async Task<bool> TryRotateRefreshTokenAsync(
        Guid tokenId,
        Guid replacementTokenId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var rotated = await _context.Set<RefreshToken>()
            .IgnoreQueryFilters()
            .Where(t => t.Id == tokenId
                && t.RevokedAt == null
                && t.ReplacedByTokenId == null
                && t.ExpiresAt > asOf)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.ReplacedByTokenId, replacementTokenId)
                    .SetProperty(t => t.RevokedAt, asOf),
                cancellationToken);

        return rotated == 1;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _context.Add(token);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Revoking the session rather than a token is what makes sign-out real and what keeps a
    /// second device working: each sign-in holds its own session, so exactly one is revoked.
    /// </remarks>
    public async Task RevokeSessionAsync(Guid sessionId, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        await _context.Set<Session>()
            .IgnoreQueryFilters()
            .Where(s => s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedAt, asOf), cancellationToken);

        await _context.Set<RefreshToken>()
            .IgnoreQueryFilters()
            .Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, asOf), cancellationToken);
    }
}
