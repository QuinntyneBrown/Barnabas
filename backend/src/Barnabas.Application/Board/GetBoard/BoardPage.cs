using Barnabas.Domain.Listings;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// A page of the board, with the counts the filter chips display.
/// </summary>
/// <remarks>
/// The counts are of the whole board rather than of the page, because they label the filters
/// rather than describe what is on screen. A parish board holds dozens of listings rather than
/// thousands, so the first page is almost always the only one - but the shape is settled here
/// rather than retrofitted when a large congregation joins.
/// </remarks>
public sealed record BoardPage(
    IReadOnlyList<BoardListingDto> Listings,
    string? NextCursor,
    IReadOnlyDictionary<ListingKind, int> Counts);
