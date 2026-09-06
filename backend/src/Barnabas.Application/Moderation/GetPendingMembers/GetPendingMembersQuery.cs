using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.GetPendingMembers;

/// <summary>Everybody in the caller's congregation waiting to be let in.</summary>
public sealed record GetPendingMembersQuery : IRequest<IReadOnlyList<PendingMemberDto>>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
