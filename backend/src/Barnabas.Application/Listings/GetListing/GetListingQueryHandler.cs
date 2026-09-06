using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.GetListing;

/// <summary>
/// Reads one listing and projects it with its owner.
/// </summary>
/// <remarks>
/// A listing belonging to another congregation has already been removed by the query filter
/// before this handler runs, so it takes the same not-found path as an identifier that exists
/// nowhere. That is what makes the two indistinguishable, which is what L2-089 asks for.
/// </remarks>
public sealed class GetListingQueryHandler : IRequestHandler<GetListingQuery, ListingDetailDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetListingQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<ListingDetailDto> Handle(GetListingQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caller = _congregation.MemberId;

        // Read the row and the owner's name in one query, then shape it here rather than in the
        // projection. The photo's address is built by PhotoUrl, which is a rule about where a
        // thing lives - not something SQL Server can be asked to concatenate.
        var found = await _context.Listings
            .Where(listing => listing.Id == request.ListingId)
            .Join(
                _context.Members,
                listing => listing.OwnerId,
                member => member.Id,
                (listing, owner) => new { Listing = listing, OwnerDisplayName = owner.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            throw new NotFoundException();
        }

        var listing = new ListingDetailDto(
            found.Listing.Id,
            found.Listing.Kind,
            found.Listing.Title,
            found.Listing.Description,
            found.Listing.Category,
            found.Listing.Neighbourhood,
            found.Listing.Status,
            found.Listing.OwnerId,
            found.OwnerDisplayName,
            found.Listing.Price,
            found.Listing.LoanTerms?.ReturnBy,
            found.Listing.Condition,
            found.Listing.AvailabilityWindows
                .OrderBy(window => window.Day)
                .ThenBy(window => window.StartsAt)
                .Select(window => new AvailabilityWindowDto(
                    window.Id,
                    window.Day,
                    window.StartsAt,
                    window.EndsAt))
                .ToList(),
            found.Listing.PostedAt,
            found.Listing.OwnerId == caller,
            found.Listing.PhotoId is { } photoId ? PhotoUrl.For(photoId, PhotoSize.Full) : null);

        return listing;
    }
}
