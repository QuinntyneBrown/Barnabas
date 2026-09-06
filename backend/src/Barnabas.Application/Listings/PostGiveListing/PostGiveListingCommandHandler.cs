using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Listings.PostLendListing;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.PostGiveListing;

/// <summary>Creates the listing and commits.</summary>
public sealed class PostGiveListingCommandHandler : IRequestHandler<PostGiveListingCommand, PostedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public PostGiveListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<PostedListingResult> Handle(
        PostGiveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = Listing.PostGive(
            Guid.NewGuid(),
            _congregation.CongregationId,
            _congregation.MemberId,
            request.Title,
            request.Description,
            request.Category,
            request.Neighbourhood,
            _time.GetUtcNow());

        _context.Listings.Add(listing);

        await _context.SaveChangesAsync(cancellationToken);

        return new PostedListingResult(listing.Id);
    }
}
