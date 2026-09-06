using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.GetModerationQueue;

/// <summary>
/// Reads the flagged listings, oldest complaint first.
/// </summary>
/// <remarks>
/// Oldest first because this is a queue and not a feed: what a moderator wants is the thing that
/// has been waiting longest, and putting the newest at the top is how the oldest complaint never
/// gets looked at.
/// <para>
/// Reports and members are joined here rather than fetched per row. The two lookups are keyed
/// dictionaries built from one read each, so a queue of thirty listings is three queries rather
/// than sixty-one.
/// </para>
/// </remarks>
public sealed class GetModerationQueueQueryHandler
    : IRequestHandler<GetModerationQueueQuery, IReadOnlyList<FlaggedListingDto>>
{
    /// <summary>As many as a moderator will work through in one sitting.</summary>
    private const int MostWaiting = 50;

    private readonly IBarnabasDbContext _context;

    public GetModerationQueueQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<IReadOnlyList<FlaggedListingDto>> Handle(
        GetModerationQueueQuery request,
        CancellationToken cancellationToken)
    {
        var flagged = await _context.Listings
            .Where(listing => listing.FlaggedAt != null)
            .OrderBy(listing => listing.FlaggedAt)
            .ThenBy(listing => listing.Id)
            .Take(MostWaiting)
            .Select(listing => new
            {
                listing.Id,
                listing.Kind,
                listing.Title,
                listing.Description,
                listing.OwnerId,
                listing.FlaggedAt,
            })
            .ToListAsync(cancellationToken);

        if (flagged.Count == 0)
        {
            return [];
        }

        var listingIds = flagged.ConvertAll(listing => listing.Id);

        var reports = await _context.ListingReports
            .Where(report => listingIds.Contains(report.ListingId) && report.ResolvedAt == null)
            .OrderBy(report => report.ReportedAt)
            .Select(report => new
            {
                report.Id,
                report.ListingId,
                report.Reason,
                report.Note,
                report.ReporterId,
                report.ReportedAt,
            })
            .ToListAsync(cancellationToken);

        var namedMemberIds = flagged.Select(listing => listing.OwnerId)
            .Concat(reports.Select(report => report.ReporterId))
            .Distinct()
            .ToList();

        var names = await _context.Members
            .Where(member => namedMemberIds.Contains(member.Id))
            .ToDictionaryAsync(member => member.Id, member => member.DisplayName, cancellationToken);

        return flagged.ConvertAll(listing => new FlaggedListingDto(
            listing.Id,
            listing.Kind,
            listing.Title,
            listing.Description,
            listing.OwnerId,
            NameOf(names, listing.OwnerId),
            listing.FlaggedAt!.Value,
            reports
                .Where(report => report.ListingId == listing.Id)
                .Select(report => new ReportedListingDto(
                    report.Id,
                    report.Reason,
                    report.Note,
                    report.ReporterId,
                    NameOf(names, report.ReporterId),
                    report.ReportedAt))
                .ToList()));
    }

    /// <summary>
    /// A member who has since left still has a name here.
    /// </summary>
    /// <remarks>
    /// The directory drops a departed member, but a complaint about their listing does not stop
    /// needing a moderator, and a queue row reading blank would be unreviewable.
    /// </remarks>
    private static string NameOf(IReadOnlyDictionary<Guid, string> names, Guid memberId) =>
        names.TryGetValue(memberId, out var name) ? name : "A member";
}
