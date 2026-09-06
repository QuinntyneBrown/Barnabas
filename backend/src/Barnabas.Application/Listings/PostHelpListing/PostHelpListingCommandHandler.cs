using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Listings.PostLendListing;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.PostHelpListing;

/// <summary>Creates the listing, mints an identifier for each window, and commits.</summary>
/// <remarks>
/// The window identifiers are minted here rather than accepted from the caller, so a request can
/// only ever name a window this listing actually declared.
/// </remarks>
public sealed class PostHelpListingCommandHandler : IRequestHandler<PostHelpListingCommand, PostedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public PostHelpListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<PostedListingResult> Handle(
        PostHelpListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var windows = request.Windows!
            .Select(window => new AvailabilityWindow(
                Guid.NewGuid(),
                window.Day!.Value,
                window.StartsAt!.Value,
                window.EndsAt!.Value))
            .ToList();

        var listing = Listing.PostHelp(
            Guid.NewGuid(),
            _congregation.CongregationId,
            _congregation.MemberId,
            request.Title,
            request.Description,
            request.Category,
            request.Neighbourhood,
            windows,
            _time.GetUtcNow());

        _context.Listings.Add(listing);

        await _context.SaveChangesAsync(cancellationToken);

        return new PostedListingResult(listing.Id);
    }
}
