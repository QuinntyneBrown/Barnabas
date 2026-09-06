using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Moderation.ApproveListing;

/// <summary>
/// A moderator has looked and found nothing wrong.
/// </summary>
/// <remarks>
/// Demands a role and no ownership. That is the whole point of moderation: the listing belongs to
/// somebody else, and a moderator who had to own it could not review anything.
/// </remarks>
public sealed record ApproveListingCommand(Guid ListingId) : IRequest<ReviewedListingResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
