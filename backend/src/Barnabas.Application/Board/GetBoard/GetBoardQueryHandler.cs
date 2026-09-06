using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// Reads the active listings of the caller's congregation and projects them for the board.
/// </summary>
/// <remarks>
/// There is no congregation predicate anywhere in this handler, and that is the point: the
/// global query filter supplies it underneath, so the safe read is the only read available.
/// </remarks>
public sealed class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, BoardPage>
{
    private readonly IBarnabasDbContext _context;

    public GetBoardQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<BoardPage> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var active = _context.Listings.Where(listing => listing.Status == ListingStatus.Active);

        var counts = await active
            .GroupBy(listing => listing.Kind)
            .Select(group => new { Kind = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.Kind, entry => entry.Count, cancellationToken);

        var page = active;

        if (request.Kind is { } kind)
        {
            page = page.Where(listing => listing.Kind == kind);
        }

        if (BoardCursor.Decode(request.Cursor) is { } cursor)
        {
            // Newest first, so a later page holds what was posted before the cursor. The
            // identifier breaks the tie when two listings share an instant.
            page = page.Where(listing =>
                listing.PostedAt < cursor.PostedAt
                || (listing.PostedAt == cursor.PostedAt && listing.Id.CompareTo(cursor.ListingId) < 0));
        }

        // One row more than asked for, purely to learn whether another page exists. It is
        // dropped before the page is returned.
        var found = await page
            .OrderByDescending(listing => listing.PostedAt)
            .ThenByDescending(listing => listing.Id)
            .Take(request.Limit + 1)
            .Join(
                _context.Members,
                listing => listing.OwnerId,
                member => member.Id,
                (listing, member) => new { Listing = listing, Owner = member })
            .ToListAsync(cancellationToken);

        var hasMore = found.Count > request.Limit;

        var listings = found
            .Take(request.Limit)
            .Select(row => new BoardListingDto(
                row.Listing.Id,
                row.Listing.Kind,
                row.Listing.Title,
                row.Owner.DisplayName,
                row.Listing.Neighbourhood,
                row.Listing.Price,

                // Help offers time rather than a thing, so a placard carries the offer in the
                // member's own words where the other kinds carry a drawing.
                row.Listing.Kind == ListingKind.Help ? row.Listing.Description : null))
            .ToList();

        var next = hasMore && listings.Count > 0
            ? new BoardCursor(found[request.Limit - 1].Listing.PostedAt, listings[^1].ListingId).Encode()
            : null;

        return new BoardPage(listings, next, counts);
    }
}
