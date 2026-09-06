using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.DeclineMember;

/// <summary>A moderator does not let an applicant in.</summary>
public sealed record DeclineMemberCommand(Guid MemberId)
    : IRequest<ApproveMember.DecidedMemberResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
