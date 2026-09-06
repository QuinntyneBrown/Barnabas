using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.Common;

/// <summary>
/// Loading a listing a moderator is deciding about, and settling its complaints.
/// </summary>
/// <remarks>
/// Shared by approving and removing because both do the same two things: find the listing through
/// the congregation filter, and close the open reports with the decision that was made. What
/// differs is what happens to the listing itself, and that stays in the two handlers.
/// </remarks>
internal static class ReviewedListing
{
    /// <summary>
    /// The flagged listing, or 404.
    /// </summary>
    /// <remarks>
    /// Read through the filtered set, so a listing in another congregation is absent before the
    /// question of moderating it arises - which is <c>L2-084 AC2</c>. A moderator's role does not
    /// widen what they can see; it widens what they may do with what they can already see.
    /// </remarks>
    public static async Task<Listing> LoadAsync(
        IBarnabasDbContext context,
        Guid listingId,
        CancellationToken cancellationToken) =>
        await context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == listingId, cancellationToken)
            ?? throw new NotFoundException();

    /// <summary>
    /// Closes every open complaint about the listing with the decision taken.
    /// </summary>
    /// <remarks>
    /// Settled rather than deleted, so a later complaint about the same listing flags it afresh
    /// without dragging the reviewed ones back into the queue behind it.
    /// </remarks>
    public static async Task ResolveReportsAsync(
        IBarnabasDbContext context,
        Guid listingId,
        ModerationOutcome outcome,
        Guid moderatorId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var open = await context.ListingReports
            .Where(report => report.ListingId == listingId && report.ResolvedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var report in open)
        {
            report.Resolve(outcome, moderatorId, asOf);
        }
    }
}
