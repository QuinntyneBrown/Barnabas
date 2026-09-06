using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.GetPendingMembers;

/// <summary>
/// Reads the applicants, the one who has waited longest first.
/// </summary>
/// <remarks>
/// Awaiting approval only. Somebody already declined is not waiting on anything, and leaving them
/// in the queue would mean a moderator declining the same person every week.
/// </remarks>
public sealed class GetPendingMembersQueryHandler
    : IRequestHandler<GetPendingMembersQuery, IReadOnlyList<PendingMemberDto>>
{
    private const int MostWaiting = 100;

    private readonly IBarnabasDbContext _context;

    public GetPendingMembersQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<IReadOnlyList<PendingMemberDto>> Handle(
        GetPendingMembersQuery request,
        CancellationToken cancellationToken) =>
        await _context.Members
            .Where(member => member.Status == MemberStatus.AwaitingApproval)
            .OrderBy(member => member.DisplayName)
            .ThenBy(member => member.Id)
            .Take(MostWaiting)
            .Select(member => new PendingMemberDto(
                member.Id,
                member.DisplayName,
                member.Neighbourhood,
                member.ReasonForJoining))
            .ToListAsync(cancellationToken);
}
