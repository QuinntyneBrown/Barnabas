using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
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

        var listing = await _context.Listings
            .Where(listing => listing.Id == request.ListingId)
            .Join(
                _context.Members,
                listing => listing.OwnerId,
                member => member.Id,
                (listing, owner) => new ListingDetailDto(
                    listing.Id,
                    listing.Kind,
                    listing.Title,
                    listing.Description,
                    listing.Category,
                    listing.Neighbourhood,
                    listing.Status,
                    listing.OwnerId,
                    owner.DisplayName,
                    listing.Price,
                    listing.LoanTerms == null ? null : listing.LoanTerms.ReturnBy,
                    listing.PostedAt,
                    listing.OwnerId == caller))
            .FirstOrDefaultAsync(cancellationToken);

        return listing ?? throw new NotFoundException();
    }
}
