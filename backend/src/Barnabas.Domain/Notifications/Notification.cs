using Barnabas.Domain.Common;

namespace Barnabas.Domain.Notifications;

/// <summary>
/// Something that happened, told to the one member it concerns.
/// </summary>
/// <remarks>
/// Every kind has its own factory, and each requires the identifiers that kind needs. That is how
/// <c>L2-074</c> is kept: a notification without a destination cannot be constructed, so there is
/// no path that produces one leading nowhere.
/// <para>
/// The destination is not stored as a URL. Where a thing lives is the client's business, and a
/// path baked into a row would be wrong the first time a route changed. What is stored is the
/// kind and the identifiers; the screen routes.
/// </para>
/// <para>
/// The other member's name and the listing's title are copied onto the row rather than joined at
/// read time. A notification is a record of what was true when it happened — a listing since
/// renamed should still say what the member was told.
/// </para>
/// </remarks>
public sealed class Notification : ITenantOwned
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid congregationId,
        Guid recipientId,
        NotificationKind kind,
        DateTimeOffset createdAt)
    {
        Id = id;
        CongregationId = congregationId;
        RecipientId = recipientId;
        Kind = kind;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    /// <summary>The one member this concerns. Never a list.</summary>
    public Guid RecipientId { get; private set; }

    public NotificationKind Kind { get; private set; }

    /// <summary>The other member it is about, so a row can name them without a second read.</summary>
    public Guid? SubjectMemberId { get; private set; }

    public string? SubjectMemberDisplayName { get; private set; }

    public Guid? ListingId { get; private set; }

    public string? ListingTitle { get; private set; }

    public Guid? RequestId { get; private set; }

    public Guid? ThreadId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsUnread => ReadAt is null;

    /// <summary>Marks it read, and leaves an already-read one alone.</summary>
    public void MarkRead(DateTimeOffset asOf) => ReadAt ??= asOf;

    /// <summary>
    /// Forgets who this was about, when that member asks to be erased.
    /// </summary>
    /// <remarks>
    /// The name is copied onto this row rather than joined, which is what makes a notification a
    /// record of what was true when it happened - and is also why erasing the member elsewhere
    /// would leave their name sitting in somebody else's list. This is where it goes.
    /// </remarks>
    public void ForgetSubject()
    {
        SubjectMemberId = null;
        SubjectMemberDisplayName = Members.Member.ErasedDisplayName;
    }

    /// <summary>Somebody has asked for something of yours.</summary>
    public static Notification RequestReceived(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        Guid requesterId,
        string requesterDisplayName,
        Guid listingId,
        string listingTitle,
        Guid requestId,
        DateTimeOffset at) =>
        new(id, congregationId, ownerId, NotificationKind.RequestReceived, at)
        {
            SubjectMemberId = requesterId,
            SubjectMemberDisplayName = requesterDisplayName,
            ListingId = listingId,
            ListingTitle = listingTitle,
            RequestId = requestId,
        };

    /// <summary>Your request was decided. An acceptance carries the thread it opened.</summary>
    public static Notification RequestDecided(
        Guid id,
        Guid congregationId,
        Guid requesterId,
        Guid ownerId,
        string ownerDisplayName,
        Guid listingId,
        string listingTitle,
        Guid requestId,
        Guid? threadId,
        bool accepted,
        DateTimeOffset at) =>
        new(
            id,
            congregationId,
            requesterId,
            accepted ? NotificationKind.RequestAccepted : NotificationKind.RequestDeclined,
            at)
        {
            SubjectMemberId = ownerId,
            SubjectMemberDisplayName = ownerDisplayName,
            ListingId = listingId,
            ListingTitle = listingTitle,
            RequestId = requestId,
            ThreadId = threadId,
        };

    /// <summary>Somebody wrote to you.</summary>
    public static Notification MessageReceived(
        Guid id,
        Guid congregationId,
        Guid recipientId,
        Guid senderId,
        string senderDisplayName,
        Guid threadId,
        Guid listingId,
        string listingTitle,
        DateTimeOffset at) =>
        new(id, congregationId, recipientId, NotificationKind.MessageReceived, at)
        {
            SubjectMemberId = senderId,
            SubjectMemberDisplayName = senderDisplayName,
            ThreadId = threadId,
            ListingId = listingId,
            ListingTitle = listingTitle,
        };

    /// <summary>A moderator took your listing off the board.</summary>
    public static Notification ListingRemoved(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        Guid listingId,
        string listingTitle,
        DateTimeOffset at) =>
        new(id, congregationId, ownerId, NotificationKind.ListingRemoved, at)
        {
            ListingId = listingId,
            ListingTitle = listingTitle,
        };
}
