namespace Barnabas.Domain.Messaging;

/// <summary>
/// One message in a thread, its sender, and when it was sent.
/// </summary>
/// <remarks>
/// Messages are the members' own words, and are presented as such — set in the serif face
/// the interface reserves for things members wrote.
/// </remarks>
public sealed class Message
{
    public const int BodyMaxLength = 4000;

    private Message()
    {
    }

    internal Message(Guid id, Guid threadId, Guid senderId, string body, DateTimeOffset sentAt)
    {
        Id = id;
        ThreadId = threadId;
        SenderId = senderId;
        Body = body;
        SentAt = sentAt;
    }

    public Guid Id { get; private set; }

    public Guid ThreadId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; }

    /// <summary>Whether this message's words have been erased at their author's request.</summary>
    public bool IsErased => Body == ErasedBody;

    /// <summary>What an erased message reads as.</summary>
    public const string ErasedBody = "This message was erased at the sender's request.";

    /// <summary>
    /// Replaces the words in place, keeping the message.
    /// </summary>
    /// <remarks>
    /// Not deleted. The other party's thread has to stay legible - <c>L2-066 AC1</c> - and a
    /// conversation with half its turns missing reads as a fault rather than as a member having
    /// exercised a right. What goes is the content; what stays is that somebody spoke.
    /// </remarks>
    public void Erase() => Body = ErasedBody;
}
