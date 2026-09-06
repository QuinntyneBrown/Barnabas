using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Moderation.Common;
using Barnabas.Domain.Moderation;
using MediatR;

namespace Barnabas.Application.Moderation.ApproveListing;

/// <summary>
/// Clears the flag and leaves the listing exactly where it was.
/// </summary>
/// <remarks>
/// Nothing here touches the status, which is what <c>L2-083</c> asks for: an approved listing is
/// still on the board because it never left it. Had the flag been a status, approving would have
/// had to guess which status to put back.
/// <para>
/// The owner is not told. They were never told it had been reported, and telling them now would
/// disclose that somebody objected - which is the half of <c>L2-081</c> that is easy to miss.
/// </para>
/// </remarks>
public sealed class ApproveListingCommandHandler
    : IRequestHandler<ApproveListingCommand, ReviewedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public ApproveListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<ReviewedListingResult> Handle(
        ApproveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await ReviewedListing.LoadAsync(_context, request.ListingId, cancellationToken);

        listing.ClearFlag();

        await ReviewedListing.ResolveReportsAsync(
            _context,
            listing.Id,
            ModerationOutcome.Approved,
            _congregation.MemberId,
            _time.GetUtcNow(),
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewedListingResult(listing.Id, listing.Status, listing.IsFlagged);
    }
}
