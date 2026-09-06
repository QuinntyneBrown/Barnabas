using Barnabas.Domain.Common;

namespace Barnabas.Domain.Messaging;

/// <summary>
/// A conversation between two members about one listing, opened by an accepted request.
/// </summary>
/// <remarks>
/// There is no way to start a conversation any other way. A member cannot message another
/// member out of the blue — a deliberate product stance rather than a missing feature. It
/// keeps the board the centre of gravity and means every thread has a subject a reader can
/// see six weeks later.
/// <para>
/// <see cref="RequestId"/> carries a uniqueness constraint, and that constraint is what
/// makes "one thread per accepted request" a fact rather than an intention.
/// </para>
/// </remarks>
public sealed class MessageThread : ITenantOwned
{
    private readonly List<Message> _messages = [];
    private readonly List<ThreadReadMark> _readMarks = [];

    private MessageThread()
    {
    }

    private MessageThread(
        Guid id,
        Guid congregationId,
        Guid requestId,
        Guid listingId,
        Guid ownerId,
        Guid requesterId,
        DateTimeOffset openedAt)
    {
        Id = id;
        CongregationId = congregationId;
        RequestId = requestId;
        ListingId = listingId;
        OwnerId = ownerId;
        RequesterId = requesterId;
        OpenedAt = openedAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    /// <summary>The request that opened it. Unique — one thread per accepted request.</summary>
    public Guid RequestId { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid RequesterId { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public IReadOnlyList<Message> Messages => _messages;

    public IReadOnlyList<ThreadReadMark> ReadMarks => _readMarks;

    /// <summary>
    /// Opens the thread an accepted request calls for. Called only by the accept handler,
    /// in the same unit of work as the acceptance.
    /// </summary>
    public static MessageThread OpenFor(
        Guid id,
        Guid congregationId,
        Guid requestId,
        Guid listingId,
        Guid ownerId,
        Guid requesterId,
        DateTimeOffset openedAt) =>
        new(id, congregationId, requestId, listingId, ownerId, requesterId, openedAt);

    /// <summary>
    /// Whether a member is one of the two the thread is between.
    /// </summary>
    /// <remarks>
    /// Party membership is asked of the entity rather than declared through
    /// <c>IRequireOwnership</c>, because a thread has two rightful actors rather than one
    /// owner. The ownership behaviour answers "did this member create it"; the question
    /// here is "is this member one of the two".
    /// </remarks>
    public bool IsParty(Guid memberId) => memberId == OwnerId || memberId == RequesterId;

    public Guid OtherParty(Guid memberId)
    {
        if (!IsParty(memberId))
        {
            throw new NotAPartyException(Id);
        }

        return memberId == OwnerId ? RequesterId : OwnerId;
    }

    public Message Append(Guid messageId, Guid senderId, string body, DateTimeOffset sentAt)
    {
        if (!IsParty(senderId))
        {
            throw new NotAPartyException(Id);
        }

        var message = new Message(messageId, Id, senderId, body, sentAt);
        _messages.Add(message);
        return message;
    }

    /// <summary>
    /// Moves the reading member's mark, and that member's alone.
    /// </summary>
    public void MarkRead(Guid readMarkId, Guid memberId, DateTimeOffset readAt)
    {
        if (!IsParty(memberId))
        {
            throw new NotAPartyException(Id);
        }

        var mark = _readMarks.SingleOrDefault(m => m.MemberId == memberId);

        if (mark is null)
        {
            _readMarks.Add(new ThreadReadMark(readMarkId, Id, memberId, readAt));
        }
        else
        {
            mark.MoveTo(readAt);
        }
    }

    /// <summary>
    /// Whether the thread holds a message the given member has not seen.
    /// </summary>
    public bool IsUnreadFor(Guid memberId)
    {
        var latest = _messages
            .Where(m => m.SenderId != memberId)
            .Select(m => (DateTimeOffset?)m.SentAt)
            .Max();

        if (latest is null)
        {
            return false;
        }

        var mark = _readMarks.SingleOrDefault(m => m.MemberId == memberId);
        return mark is null || mark.ReadAt < latest;
    }
}
