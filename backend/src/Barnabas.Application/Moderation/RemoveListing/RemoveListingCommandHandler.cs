using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Moderation.ApproveListing;
using Barnabas.Application.Moderation.Common;
using Barnabas.Application.Notifications.Common;
using Barnabas.Domain.Moderation;
using MediatR;

namespace Barnabas.Application.Moderation.RemoveListing;

/// <summary>
/// Takes the listing off the board and tells its owner.
/// </summary>
/// <remarks>
/// The status becomes <c>Removed</c> rather than <c>Archived</c>. Archiving is the owner shelving
/// their own listing and <c>L2-039</c> lets them put it back; a moderator's removal is a decision
/// made about them, and reusing the same status would have handed the owner an undo button.
/// <para>
/// The notification is staged before the save, into this handler's own unit of work, so a removal
/// that fails leaves no message saying it happened. The owner is told what was removed and not who
/// objected - <c>L2-081</c>.
/// </para>
/// </remarks>
public sealed class RemoveListingCommandHandler
    : IRequestHandler<RemoveListingCommand, ReviewedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public RemoveListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        INotifier notifier,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _notifier = notifier;
        _time = time;
    }

    public async Task<ReviewedListingResult> Handle(
        RemoveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await ReviewedListing.LoadAsync(_context, request.ListingId, cancellationToken);
        var now = _time.GetUtcNow();

        listing.RemoveByModerator(now);

        await ReviewedListing.ResolveReportsAsync(
            _context,
            listing.Id,
            ModerationOutcome.Removed,
            _congregation.MemberId,
            now,
            cancellationToken);

        await _notifier.ListingRemovedAsync(listing, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewedListingResult(listing.Id, listing.Status, listing.IsFlagged);
    }
}
