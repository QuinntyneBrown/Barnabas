using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.ArchiveListing;

public sealed record ArchivedListingResult(Guid ListingId, ListingStatus Status);
