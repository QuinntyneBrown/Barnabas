using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Search.SearchListings;

/// <summary>
/// Searches the active listings of the caller's own congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The global query filter supplies that predicate, which is the whole
/// of <c>L2-088 AC2</c> — a term matching another parish's listing simply has nothing to match
/// against, with no comparison here for a handler to forget.
/// </remarks>
public sealed record SearchListingsQuery(
    string Term,
    ListingKind? Kind,
    string? Neighbourhood,
    decimal? MaxPrice,
    int Limit,
    string? Cursor) : IRequest<SearchPage>;
