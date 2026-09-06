using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.GetListing;

/// <summary>
/// One listing, as its own screen shows it.
/// </summary>
/// <remarks>
/// <see cref="IsOwnedByCaller"/> is answered here rather than left to the screen to work out by
/// comparing identifiers. The owner sees what they can do with a listing and a visitor sees a way
/// to ask for it, and those are different screens: deciding which on the server means the two
/// cannot drift apart, and means the screen never has to hold the caller's own identifier to
/// render correctly.
/// </remarks>
public sealed record ListingDetailDto(
    Guid ListingId,
    ListingKind Kind,
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    ListingStatus Status,
    Guid OwnerId,
    string OwnerDisplayName,
    decimal? Price,
    DateOnly? ReturnBy,
    DateTimeOffset PostedAt,
    bool IsOwnedByCaller);
