using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.ApproveMember;

/// <summary>A moderator lets an applicant in.</summary>
public sealed record ApproveMemberCommand(Guid MemberId) : IRequest<DecidedMemberResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
