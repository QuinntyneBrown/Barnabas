using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.GetMyListings;

/// <summary>
/// Reads the caller's listings and counts what is waiting on each, in one round trip.
/// </summary>
/// <remarks>
/// The count is a correlated subquery in the projection rather than a second pass. Counting per
/// row would turn one screen into one query per listing, and this is the screen an owner opens
/// most often.
/// </remarks>
public sealed class GetMyListingsQueryHandler : IRequestHandler<GetMyListingsQuery, IReadOnlyList<MyListingDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetMyListingsQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<MyListingDto>> Handle(
        GetMyListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owner = _congregation.MemberId;
        var requests = _context.ListingRequests;

        return await _context.Listings
            .Where(listing => listing.OwnerId == owner)
            .Where(listing => request.IncludeClosed || listing.Status == ListingStatus.Active)
            .OrderByDescending(listing => listing.PostedAt)
            .Select(listing => new MyListingDto(
                listing.Id,
                listing.Kind,
                listing.Status,
                listing.Title,
                listing.Price,
                requests.Count(r => r.ListingId == listing.Id && r.Status == RequestStatus.Pending),
                listing.Status == ListingStatus.Archived && listing.ClosedOutAt == null))
            .ToListAsync(cancellationToken);
    }
}
