using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.RestoreListing;

/// <summary>
/// Puts a shelved listing back on the board.
/// </summary>
/// <remarks>
/// Only a listing that was archived rather than closed out. A returned loan is archived too, and
/// restoring it would reopen something already finished - the entity refuses that.
/// </remarks>
public sealed record RestoreListingCommand(Guid ListingId)
    : IRequest<RestoredListingResult>, IRequireOwnership<Listing>
{
    public Guid ResourceId => ListingId;
}
