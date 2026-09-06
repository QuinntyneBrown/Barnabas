using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;

namespace Barnabas.Application.Notifications.Common;

/// <summary>
/// Tells the one member each event concerns.
/// </summary>
/// <remarks>
/// Every method stages a row into the caller's own unit of work and none of them saves. That is
/// the whole design: a notification is written in the same <c>SaveChangesAsync</c> as the fact it
/// describes, so a request refused by the filtered unique index and an acceptance refused by the
/// row version both leave nothing behind.
/// <para>
/// Not a pipeline behaviour, which sees the command and the result but not the domain facts —
/// it would have to re-read the listing's owner after the transaction committed. Not a
/// post-commit publish either, which would leave a window in which an accepted request has no
/// notification.
/// </para>
/// </remarks>
public interface INotifier
{
    /// <summary>Somebody has asked for something of this listing owner's.</summary>
    Task RequestMadeAsync(Listing listing, ListingRequest request, CancellationToken cancellationToken);

    /// <summary>The owner has decided. An acceptance carries the thread it opened.</summary>
    Task RequestDecidedAsync(
        Listing listing,
        ListingRequest request,
        Guid? threadId,
        bool accepted,
        CancellationToken cancellationToken);

    /// <summary>Somebody has written to the other party in a thread.</summary>
    Task MessageSentAsync(
        Guid threadId,
        Guid listingId,
        string listingTitle,
        Guid senderId,
        Guid recipientId,
        CancellationToken cancellationToken);

    /// <summary>A moderator has taken a listing off the board.</summary>
    Task ListingRemovedAsync(Listing listing, CancellationToken cancellationToken);
}
