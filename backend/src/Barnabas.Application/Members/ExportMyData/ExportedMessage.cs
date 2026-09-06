namespace Barnabas.Application.Members.ExportMyData;

/// <summary>One message this member sent. The other party's words are the other party's data.</summary>
public sealed record ExportedMessage(
    Guid MessageId,
    Guid ThreadId,
    string Body,
    DateTimeOffset SentAt);
