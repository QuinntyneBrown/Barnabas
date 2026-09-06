using Barnabas.Domain.Listings;

namespace Barnabas.Application.Moderation.ApproveListing;

/// <summary>What a moderator's decision left behind.</summary>
public sealed record ReviewedListingResult(Guid ListingId, ListingStatus Status, bool Flagged);
