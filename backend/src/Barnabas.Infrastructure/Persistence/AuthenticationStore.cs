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

    /// <summary>
    /// The instant, in the shape this provider stores it.
    /// </summary>
    /// <remarks>
    /// The conditional updates below are written as SQL, which goes around the value converters
    /// the model applies. On SQLite timestamps are stored as UTC DateTime so they can be sorted,
    /// so a raw statement has to write one too, or the value it writes will not read back.
    /// </remarks>
    private object Timestamp(DateTimeOffset value) =>
        _context.Database.IsSqlite() ? value.UtcDateTime : value;

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
    /// <para>
    /// The returned token may still be expired. Consumption and expiry are separate questions -
    /// only the first is a race - so the caller asks the entity about the second.
    /// </para>
    /// </remarks>
    public async Task<SignInToken?> TryConsumeSignInTokenAsync(
        string tokenHash,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        // Written as SQL rather than through the query API because the guarantee wanted here is
        // a property of the statement: one UPDATE, and the row count it reports is the answer.
        // Only the consumption is raced over. Expiry is checked below, on the entity, because a
        // clock reading is not something two callers can disagree about.
        var consumed = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "SignInTokens"
             SET "ConsumedAt" = {Timestamp(asOf)}
             WHERE "TokenHash" = {tokenHash} AND "ConsumedAt" IS NULL
             """,
            cancellationToken);

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
    /// requires. Expiry is the caller's question, for the same reason.
    /// </remarks>
    public async Task<bool> TryRotateRefreshTokenAsync(
        Guid tokenId,
        Guid replacementTokenId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var rotated = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "RefreshTokens"
             SET "ReplacedByTokenId" = {replacementTokenId}, "RevokedAt" = {Timestamp(asOf)}
             WHERE "Id" = {tokenId} AND "RevokedAt" IS NULL AND "ReplacedByTokenId" IS NULL
             """,
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
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "Sessions" SET "RevokedAt" = {Timestamp(asOf)}
             WHERE "Id" = {sessionId} AND "RevokedAt" IS NULL
             """,
            cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "RefreshTokens" SET "RevokedAt" = {Timestamp(asOf)}
             WHERE "SessionId" = {sessionId} AND "RevokedAt" IS NULL
             """,
            cancellationToken);
    }
}
