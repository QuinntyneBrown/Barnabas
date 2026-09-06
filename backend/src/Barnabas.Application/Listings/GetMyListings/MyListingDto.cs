using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.GetMyListings;

/// <summary>
/// One of the caller's own listings, with how many people are waiting on it.
/// </summary>
/// <remarks>
/// The open request count is the reason this screen exists rather than being a filtered board:
/// what an owner needs to know is which of their listings somebody is waiting on.
/// </remarks>
public sealed record MyListingDto(
    Guid ListingId,
    ListingKind Kind,
    ListingStatus Status,
    string Title,
    decimal? Price,
    int OpenRequestCount);
