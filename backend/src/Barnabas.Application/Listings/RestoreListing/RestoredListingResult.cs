using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.RestoreListing;

public sealed record RestoredListingResult(Guid ListingId, ListingStatus Status);
