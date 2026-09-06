using Barnabas.Domain.Listings;

namespace Barnabas.Application.Search.SearchListings;

/// <summary>One listing a search found.</summary>
public sealed record SearchResultDto(
    Guid ListingId,
    ListingKind Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price);
