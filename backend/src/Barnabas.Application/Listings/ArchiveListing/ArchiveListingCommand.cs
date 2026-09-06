using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.ArchiveListing;

/// <summary>
/// Takes a listing off the board without closing it out.
/// </summary>
/// <remarks>
/// Distinct from close-out, and deliberately so. Closing out records that the thing has gone;
/// archiving records only that the owner has stopped offering it, and it can be undone.
/// </remarks>
public sealed record ArchiveListingCommand(Guid ListingId)
    : IRequest<ArchivedListingResult>, IRequireOwnership<Listing>
{
    public Guid ResourceId => ListingId;
}
