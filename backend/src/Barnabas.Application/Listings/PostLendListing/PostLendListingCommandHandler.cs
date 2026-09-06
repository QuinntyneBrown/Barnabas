using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.PostLendListing;

/// <summary>Creates the listing and commits.</summary>
/// <remarks>
/// The owner and the congregation come from the session, and the congregation is stamped again
/// on save, so nothing a caller can write reaches either.
/// </remarks>
public sealed class PostLendListingCommandHandler : IRequestHandler<PostLendListingCommand, PostedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public PostLendListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<PostedListingResult> Handle(
        PostLendListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = Listing.PostLend(
            Guid.NewGuid(),
            _congregation.CongregationId,
            _congregation.MemberId,
            request.Title,
            request.Description,
            request.Category,
            request.Neighbourhood,
            request.ReturnBy!.Value,
            _time.GetUtcNow());

        _context.Listings.Add(listing);

        await _context.SaveChangesAsync(cancellationToken);

        return new PostedListingResult(listing.Id);
    }
}
