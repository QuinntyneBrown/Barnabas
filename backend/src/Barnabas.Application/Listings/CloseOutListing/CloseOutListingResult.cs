using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.CloseOutListing;

/// <summary>The outcome the entity chose, so the screen can say what happened.</summary>
public sealed record CloseOutListingResult(Guid ListingId, ListingStatus Status);
