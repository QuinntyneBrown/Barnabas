using Barnabas.Application.Board.GetBoard;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Search.SearchListings;

/// <summary>
/// Finds active listings whose title or description carries the term.
/// </summary>
/// <remarks>
/// Case-insensitivity is a property of the column's collation rather than of a <c>ToLower()</c>
/// on both sides. Lowering the column would defeat the index that serves the board, and lowering
/// only the term would leave the answer depending on whichever collation the machine happened to
/// be installed with — which is the argument ADR-0001 makes about constraints, applied to a
/// comparison.
/// <para>
/// The congregation predicate comes from the global filter and appears nowhere here. That is what
/// makes <c>L2-088 AC2</c> true: there is no line to forget, because there is no line.
/// </para>
/// </remarks>
public sealed class SearchListingsQueryHandler : IRequestHandler<SearchListingsQuery, SearchPage>
{
    private readonly IBarnabasDbContext _context;

    public SearchListingsQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<SearchPage> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var term = request.Term.Trim();
        var pattern = LikePattern.Containing(term);

        // Active only, so a sold or shelved listing never surfaces - L2-053. Applied first,
        // because it is the cheapest predicate and the one the index is built for.
        var results = _context.Listings.Where(listing => listing.Status == ListingStatus.Active);

        results = results.Where(listing =>
            EF.Functions.Like(listing.Title, pattern, LikePattern.Escape)
            || EF.Functions.Like(listing.Description, pattern, LikePattern.Escape));

        if (request.Kind is { } kind)
        {
            results = results.Where(listing => listing.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(request.Neighbourhood))
        {
            var neighbourhood = request.Neighbourhood.Trim();

            results = results.Where(listing => listing.Neighbourhood == neighbourhood);
        }

        if (request.MaxPrice is { } cap)
        {
            // The null test is L2-051 AC2 in one line: only a Sell listing carries a price, so a
            // price filter excludes Lend, Give and Help without naming a kind. Somebody filtering
            // by price is shopping, and a free ladder is not an answer to that.
            results = results.Where(listing => listing.Price != null && listing.Price <= cap);
        }

        if (BoardCursor.Decode(request.Cursor) is { } cursor)
        {
            results = results.Where(listing =>
                listing.PostedAt < cursor.PostedAt
                || (listing.PostedAt == cursor.PostedAt && listing.Id.CompareTo(cursor.ListingId) < 0));
        }

        // One more than asked for, purely to learn whether another page exists.
        var found = await results
            .OrderByDescending(listing => listing.PostedAt)
            .ThenByDescending(listing => listing.Id)
            .Take(request.Limit + 1)
            .Join(
                _context.Members,
                listing => listing.OwnerId,
                member => member.Id,
                (listing, owner) => new { Listing = listing, Owner = owner })
            .ToListAsync(cancellationToken);

        var hasMore = found.Count > request.Limit;

        var page = found
            .Take(request.Limit)
            .Select(row => new SearchResultDto(
                row.Listing.Id,
                row.Listing.Kind,
                row.Listing.Title,
                row.Owner.DisplayName,
                row.Listing.Neighbourhood,
                row.Listing.Price))
            .ToList();

        var next = hasMore && page.Count > 0
            ? new BoardCursor(found[request.Limit - 1].Listing.PostedAt, page[^1].ListingId).Encode()
            : null;

        return new SearchPage(term, page, next);
    }
}
