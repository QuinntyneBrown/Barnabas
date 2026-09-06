namespace Barnabas.Application.Requests.AcceptRequest;

/// <summary>
/// The decision, and the thread it opened.
/// </summary>
/// <remarks>
/// The thread identifier is returned so the confirmation can offer the conversation directly.
/// Accepting is the moment two members are put in touch, and making them go and find the thread
/// afterwards would be a strange place to stop.
/// </remarks>
public sealed record AcceptRequestResult(Guid RequestId, Guid ThreadId);
