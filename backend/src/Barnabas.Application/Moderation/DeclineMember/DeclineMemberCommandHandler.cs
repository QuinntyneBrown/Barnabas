using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Moderation.ApproveMember;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.DeclineMember;

/// <summary>
/// Declines the applicant.
/// </summary>
/// <remarks>
/// The record is kept rather than deleted. A declined applicant who redeemed another code would
/// otherwise start again as though nothing had happened, and the moderators would have no way to
/// know they had already decided.
/// <para>
/// Confirmation is the screen's business - <c>L2-087 AC2</c> is about the dialogue in front of the
/// moderator, not about a second call to the API.
/// </para>
/// </remarks>
public sealed class DeclineMemberCommandHandler : IRequestHandler<DeclineMemberCommand, DecidedMemberResult>
{
    private readonly IBarnabasDbContext _context;

    public DeclineMemberCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task<DecidedMemberResult> Handle(
        DeclineMemberCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await _context.Members
            .FirstOrDefaultAsync(candidate => candidate.Id == request.MemberId, cancellationToken)
            ?? throw new NotFoundException();

        member.Decline();

        await _context.SaveChangesAsync(cancellationToken);

        return new DecidedMemberResult(member.Id, member.Status);
    }
}
