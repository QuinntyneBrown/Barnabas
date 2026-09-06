using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.RemoveListing;

/// <summary>
/// A moderator takes a listing off the board.
/// </summary>
/// <remarks>
/// A separate command from the owner's own withdrawal rather than a flag on it, and it declares
/// <see cref="IRequireRole"/> without <c>IRequireOwnership</c>. That is how <c>L2-041</c>'s "or by
/// a moderator acting under L2-084" is honoured without weakening the ownership check that
/// protects every other listing action.
/// </remarks>
public sealed record RemoveListingCommand(Guid ListingId)
    : IRequest<ApproveListing.ReviewedListingResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
