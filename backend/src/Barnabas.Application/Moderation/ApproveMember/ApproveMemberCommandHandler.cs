using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.ApproveMember;

/// <summary>
/// Approves the applicant.
/// </summary>
/// <remarks>
/// Nothing here touches a session. The member's role and status are restamped from their record on
/// every request rather than read from the token issued at sign-in, so somebody who signed in
/// while pending sees the board on their next visit instead of having to sign in again -
/// <c>L2-086 AC3</c>.
/// </remarks>
public sealed class ApproveMemberCommandHandler : IRequestHandler<ApproveMemberCommand, DecidedMemberResult>
{
    private readonly IBarnabasDbContext _context;

    public ApproveMemberCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task<DecidedMemberResult> Handle(
        ApproveMemberCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Through the filtered set, so an applicant to another congregation is not found rather
        // than refused - a moderator may not approve their way across the boundary.
        var member = await _context.Members
            .FirstOrDefaultAsync(candidate => candidate.Id == request.MemberId, cancellationToken)
            ?? throw new NotFoundException();

        member.Approve();

        await _context.SaveChangesAsync(cancellationToken);

        return new DecidedMemberResult(member.Id, member.Status);
    }
}
