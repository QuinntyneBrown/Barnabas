using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class InvitationStore : IInvitationStore
{
    private readonly BarnabasDbContext _context;

    public InvitationStore(BarnabasDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<InviteCode?> TryRedeemAsync(
        string normalisedCode,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        // One UPDATE, and the row count it reports is the answer. A read followed by a save cannot
        // promise what L2-009 asks for: two callers can both find an unredeemed code and both
        // proceed. Here the database refuses the second, and that caller gets the same 410 a
        // genuinely reused code gets.
        var redeemed = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE [InviteCodes]
             SET [RedeemedAt] = {asOf}
             WHERE [Code] = {normalisedCode} AND [RedeemedAt] IS NULL
             """,
            cancellationToken);

        if (redeemed == 0)
        {
            return null;
        }

        return await FindCodeAsync(normalisedCode, cancellationToken);
    }

    public Task<InviteCode?> FindCodeAsync(string normalisedCode, CancellationToken cancellationToken) =>
        _context.Set<InviteCode>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(code => code.Code == normalisedCode, cancellationToken);

    public async Task AddJoiningSessionAsync(JoiningSession session, CancellationToken cancellationToken)
    {
        _context.Add(session);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<JoiningSession?> TryConsumeJoiningSessionAsync(
        string tokenHash,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var consumed = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE [JoiningSessions]
             SET [ConsumedAt] = {asOf}
             WHERE [TokenHash] = {tokenHash} AND [ConsumedAt] IS NULL
             """,
            cancellationToken);

        if (consumed == 0)
        {
            return null;
        }

        return await _context.Set<JoiningSession>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);
    }

    public Task<Congregation?> FindCongregationAsync(Guid congregationId, CancellationToken cancellationToken) =>
        _context.Congregations
            .AsNoTracking()
            .FirstOrDefaultAsync(congregation => congregation.Id == congregationId, cancellationToken);

    public Task<bool> EmailIsRegisteredAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var normalised = Member.Normalise(emailAddress);

        return _context.Set<Member>()
            .IgnoreQueryFilters()
            .AnyAsync(member => member.EmailAddress == normalised, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddMemberAsync(Member member, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(member);

        // Named in full so a schema change breaks this loudly at the first acceptance test rather
        // than quietly writing the wrong shape. The congregation comes from the joining session,
        // not from the caller, and there is no session to stamp it from anyway.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO [Members] ([Id], [CongregationId], [EmailAddress], [DisplayName], [Neighbourhood], [Role], [Status], [ReasonForJoining])
             VALUES ({member.Id}, {member.CongregationId}, {member.EmailAddress}, {member.DisplayName}, {member.Neighbourhood}, {(int)member.Role}, {(int)member.Status}, {member.ReasonForJoining})
             """,
            cancellationToken);
    }
}
