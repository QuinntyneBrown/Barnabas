using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.DeleteListing;

/// <summary>
/// Removes a listing and everything that only existed because of it.
/// </summary>
/// <remarks>
/// Irreversible, and only from the archive. Requiring the listing to be shelved first puts a
/// deliberate step between taking something off the board and destroying it, so a member cannot
/// delete an active listing that people are waiting on with one action.
/// </remarks>
public sealed record DeleteListingCommand(Guid ListingId) : IRequest, IRequireOwnership<Listing>
{
    public Guid ResourceId => ListingId;
}
