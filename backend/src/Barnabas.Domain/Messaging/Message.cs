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
}
