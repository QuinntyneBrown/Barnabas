namespace Barnabas.Domain.Messaging;

/// <summary>
/// Raised when a member who is not a party reaches for a thread.
/// </summary>
/// <remarks>
/// Mapped to 404, not 403 — the same reasoning that governs cross-congregation access.
/// A 403 would confirm the thread exists, which is itself a disclosure.
/// </remarks>
public sealed class NotAPartyException : Exception
{
    public NotAPartyException(Guid threadId)
        : base("The thread was not found.")
        => ThreadId = threadId;

    public Guid ThreadId { get; }
}
