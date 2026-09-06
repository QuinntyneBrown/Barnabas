using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Requests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.GetMyRequests;

/// <summary>
/// Reads what the caller asked for, newest first, with the thread where one was opened.
/// </summary>
/// <remarks>
/// One predicate: the caller is the requester. The congregation predicate comes from the filter
/// underneath, so this handler adds one condition rather than three.
/// </remarks>
public sealed class GetMyRequestsQueryHandler : IRequestHandler<GetMyRequestsQuery, IReadOnlyList<MyRequestDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetMyRequestsQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<MyRequestDto>> Handle(
        GetMyRequestsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requester = _congregation.MemberId;
        var threads = _context.MessageThreads;

        var query = _context.ListingRequests.Where(listingRequest => listingRequest.RequesterId == requester);

        if (request.Status is { } status)
        {
            query = query.Where(listingRequest => listingRequest.Status == status);
        }

        var rows = await query
            .Join(
                _context.Listings,
                listingRequest => listingRequest.ListingId,
                listing => listing.Id,
                (listingRequest, listing) => new { Request = listingRequest, Listing = listing })
            .Join(
                _context.Members,
                row => row.Listing.OwnerId,
                member => member.Id,
                (row, owner) => new
                {
                    row.Request,
                    row.Listing,
                    Owner = owner,
                    ThreadId = threads
                        .Where(thread => thread.RequestId == row.Request.Id)
                        .Select(thread => (Guid?)thread.Id)
                        .FirstOrDefault(),
                })
            .OrderByDescending(row => row.Request.MadeAt)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new MyRequestDto(
            row.Request.Id,
            row.Listing.Id,
            row.Listing.Title,
            row.Listing.Kind,
            row.Owner.DisplayName,
            row.Listing.Neighbourhood,
            LoanTermsText.For(row.Request.LoanTerms),
            row.Request.Status,
            row.Request.MadeAt,
            row.ThreadId))];
    }
}
