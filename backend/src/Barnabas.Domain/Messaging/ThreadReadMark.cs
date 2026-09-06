namespace Barnabas.Domain.Messaging;

/// <summary>
/// How far one member has read one thread.
/// </summary>
/// <remarks>
/// Unread is a property of the reader rather than of the thread: two members looking at
/// the same thread can disagree about whether it is unread, so opening a thread marks it
/// read for that member alone.
/// </remarks>
public sealed class ThreadReadMark
{
    private ThreadReadMark()
    {
    }

    internal ThreadReadMark(Guid id, Guid threadId, Guid memberId, DateTimeOffset readAt)
    {
        Id = id;
        ThreadId = threadId;
        MemberId = memberId;
        ReadAt = readAt;
    }

    public Guid Id { get; private set; }

    public Guid ThreadId { get; private set; }

    public Guid MemberId { get; private set; }

    public DateTimeOffset ReadAt { get; private set; }

    internal void MoveTo(DateTimeOffset readAt)
    {
        if (readAt > ReadAt)
        {
            ReadAt = readAt;
        }
    }
}
