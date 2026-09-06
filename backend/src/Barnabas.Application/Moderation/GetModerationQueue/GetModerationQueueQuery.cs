using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.GetModerationQueue;

/// <summary>
/// Every flagged listing in the caller's congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The one it reads comes from the verified session, so a moderator of
/// one parish has no way to ask about another - <c>L2-082</c> and <c>L2-088</c> are the same
/// mechanism here.
/// </remarks>
public sealed record GetModerationQueueQuery : IRequest<IReadOnlyList<FlaggedListingDto>>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
