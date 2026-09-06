using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.CloseOutListing;

/// <summary>
/// Records that a listing has gone, in the vocabulary of its kind.
/// </summary>
/// <remarks>
/// It carries no outcome. A Sell listing closes as sold, a Give as given away, a Lend as
/// archived and a Help as completed, and the entity decides which from its own kind - so no
/// caller can mark a gift as sold, and the screen's wording and the stored outcome cannot
/// disagree.
/// </remarks>
public sealed record CloseOutListingCommand(Guid ListingId)
    : IRequest<CloseOutListingResult>, IRequireOwnership<Listing>
{
    public Guid ResourceId => ListingId;
}
