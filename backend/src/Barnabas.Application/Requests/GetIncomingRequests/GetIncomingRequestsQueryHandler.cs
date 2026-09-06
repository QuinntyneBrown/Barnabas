using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Requests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.GetIncomingRequests;

/// <summary>
/// Reads what has been asked of the caller, newest first.
/// </summary>
/// <remarks>
/// Projected in one pass. The requester's display name and the listing's title live on other
/// aggregates, and fetching them per row would turn one screen into a query per request.
/// </remarks>
public sealed class GetIncomingRequestsQueryHandler
    : IRequestHandler<GetIncomingRequestsQuery, IReadOnlyList<IncomingRequestDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetIncomingRequestsQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<IncomingRequestDto>> Handle(
        GetIncomingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var owner = _congregation.MemberId;

        var rows = await _context.ListingRequests
            .Join(
                _context.Listings.Where(listing => listing.OwnerId == owner),
                listingRequest => listingRequest.ListingId,
                listing => listing.Id,
                (listingRequest, listing) => new { Request = listingRequest, Listing = listing })
            .Join(
                _context.Members,
                row => row.Request.RequesterId,
                member => member.Id,
                (row, requester) => new { row.Request, row.Listing, Requester = requester })
            .OrderByDescending(row => row.Request.MadeAt)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new IncomingRequestDto(
            row.Request.Id,
            row.Requester.Id,
            row.Requester.DisplayName,
            row.Listing.Id,
            row.Listing.Title,
            row.Listing.Kind,
            row.Request.Message,
            LoanTermsText.For(row.Request.LoanTerms),
            row.Request.Status,
            row.Request.MadeAt))];
    }
}
