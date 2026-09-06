using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Messaging.GetMyThreads;

/// <summary>
/// Reads the caller's conversations, most recently active first.
/// </summary>
/// <remarks>
/// The predicate is that the caller is one of the two parties. The congregation predicate comes
/// from the filter underneath, so another congregation's thread is invisible before this handler
/// is even consulted.
/// </remarks>
public sealed class GetMyThreadsQueryHandler : IRequestHandler<GetMyThreadsQuery, IReadOnlyList<ThreadSummaryDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetMyThreadsQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<ThreadSummaryDto>> Handle(
        GetMyThreadsQuery request,
        CancellationToken cancellationToken)
    {
        var caller = _congregation.MemberId;

        var threads = await _context.MessageThreads
            .Where(thread => thread.OwnerId == caller || thread.RequesterId == caller)
            .Include(thread => thread.Messages)
            .Include(thread => thread.ReadMarks)
            .ToListAsync(cancellationToken);

        var listingTitles = await _context.Listings
            .Where(listing => threads.Select(thread => thread.ListingId).Contains(listing.Id))
            .ToDictionaryAsync(listing => listing.Id, listing => listing.Title, cancellationToken);

        var otherIds = threads.Select(thread => thread.OtherParty(caller)).ToList();

        var names = await _context.Members
            .Where(member => otherIds.Contains(member.Id))
            .ToDictionaryAsync(member => member.Id, member => member.DisplayName, cancellationToken);

        return
        [
            .. threads
                .Select(thread =>
                {
                    var latest = thread.Messages.OrderByDescending(message => message.SentAt).FirstOrDefault();
                    var other = thread.OtherParty(caller);

                    return new ThreadSummaryDto(
                        thread.Id,
                        other,
                        names.GetValueOrDefault(other, string.Empty),
                        thread.ListingId,
                        listingTitles.GetValueOrDefault(thread.ListingId, string.Empty),
                        latest?.Body ?? string.Empty,
                        latest?.SentAt,
                        thread.IsUnreadFor(caller));
                })
                .OrderByDescending(summary => summary.LatestAt ?? DateTimeOffset.MinValue),
        ];
    }
}
