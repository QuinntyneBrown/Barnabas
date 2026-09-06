namespace Barnabas.Application.Listings.EditListing;

/// <summary>What was changed, so the screen can go back to the listing it just corrected.</summary>
public sealed record EditedListingResult(Guid ListingId, string Title);
