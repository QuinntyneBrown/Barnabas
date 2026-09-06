namespace Barnabas.Application.Messaging.GetThread;

/// <summary>
/// One message, attributed.
/// </summary>
/// <remarks>
/// <see cref="SentByCaller"/> is decided here rather than left to the screen to work out by
/// comparing identifiers, for the same reason the listing says whether the caller owns it: the
/// screen renders the member's own words differently from the other party's, and that decision
/// should not depend on the client holding its own identifier.
/// </remarks>
public sealed record MessageDto(
    Guid MessageId,
    Guid SenderId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset SentAt,
    bool SentByCaller);
