using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Joining.IssueInviteCode;

/// <summary>
/// Issues a code for the moderator's own congregation.
/// </summary>
/// <remarks>
/// It names no congregation. That comes from the verified session, so a moderator cannot issue a
/// code that lets somebody into a parish they do not belong to — which is half of <c>L2-090</c>.
/// </remarks>
public sealed record IssueInviteCodeCommand : IRequest<IssuedInviteCodeResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
