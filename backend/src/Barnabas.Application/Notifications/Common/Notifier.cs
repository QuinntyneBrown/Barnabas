using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Notifications;
using Barnabas.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.Common;

/// <inheritdoc />
public sealed class Notifier : INotifier
{
    private readonly IBarnabasDbContext _context;
    private readonly TimeProvider _time;

    public Notifier(IBarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task RequestMadeAsync(
        Listing listing,
        ListingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(request);

        var requester = await NameOfAsync(request.RequesterId, cancellationToken);

        await StageAsync(
            listing.OwnerId,
            NotificationKind.RequestReceived,
            id => Notification.RequestReceived(
                id,
                listing.CongregationId,
                listing.OwnerId,
                request.RequesterId,
                requester,
                listing.Id,
                listing.Title,
                request.Id,
                _time.GetUtcNow()),
            cancellationToken);
    }

    public async Task RequestDecidedAsync(
        Listing listing,
        ListingRequest request,
        Guid? threadId,
        bool accepted,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(request);

        var owner = await NameOfAsync(listing.OwnerId, cancellationToken);

        await StageAsync(
            request.RequesterId,
            accepted ? NotificationKind.RequestAccepted : NotificationKind.RequestDeclined,
            id => Notification.RequestDecided(
                id,
                listing.CongregationId,
                request.RequesterId,
                listing.OwnerId,
                owner,
                listing.Id,
                listing.Title,
                request.Id,
                threadId,
                accepted,
                _time.GetUtcNow()),
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The recipient is the other party, and the sender is never told about their own message —
    /// which is <c>L2-072</c>, and is settled by the caller handing over one recipient rather
    /// than by filtering a list afterwards.
    /// </remarks>
    public async Task MessageSentAsync(
        Guid threadId,
        Guid listingId,
        string listingTitle,
        Guid senderId,
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        var sender = await NameOfAsync(senderId, cancellationToken);
        var congregationId = await CongregationOfAsync(recipientId, cancellationToken);

        await StageAsync(
            recipientId,
            NotificationKind.MessageReceived,
            id => Notification.MessageReceived(
                id,
                congregationId,
                recipientId,
                senderId,
                sender,
                threadId,
                listingId,
                listingTitle,
                _time.GetUtcNow()),
            cancellationToken);
    }

    public async Task ListingRemovedAsync(Listing listing, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listing);

        await StageAsync(
            listing.OwnerId,
            NotificationKind.ListingRemoved,
            id => Notification.ListingRemoved(
                id,
                listing.CongregationId,
                listing.OwnerId,
                listing.Id,
                listing.Title,
                _time.GetUtcNow()),
            cancellationToken);
    }

    /// <summary>
    /// Adds the row, unless the recipient has said they do not want this kind.
    /// </summary>
    /// <remarks>
    /// The preference is read at the moment of creation, so a disabled kind is never
    /// <em>created</em> rather than merely hidden — <c>L2-075 AC1</c> asks for the first, and
    /// filtering at the read would leave the row in the database and the count wrong.
    /// <para>
    /// Nothing is saved here. The row joins whatever the calling handler is about to commit.
    /// </para>
    /// </remarks>
    private async Task StageAsync(
        Guid recipientId,
        NotificationKind kind,
        Func<Guid, Notification> build,
        CancellationToken cancellationToken)
    {
        var chosen = await _context.NotificationPreferences
            .Where(preference => preference.MemberId == recipientId)
            .ToDictionaryAsync(
                preference => preference.Kind,
                preference => preference.Enabled,
                cancellationToken);

        if (!new NotificationPreferences(chosen).Allows(kind))
        {
            return;
        }

        _context.Notifications.Add(build(Guid.NewGuid()));
    }

    private async Task<string> NameOfAsync(Guid memberId, CancellationToken cancellationToken) =>
        await _context.Members
            .Where(member => member.Id == memberId)
            .Select(member => member.DisplayName)
            .FirstOrDefaultAsync(cancellationToken) ?? "A member";

    private async Task<Guid> CongregationOfAsync(Guid memberId, CancellationToken cancellationToken) =>
        await _context.Members
            .Where(member => member.Id == memberId)
            .Select(member => member.CongregationId)
            .FirstOrDefaultAsync(cancellationToken);
}
