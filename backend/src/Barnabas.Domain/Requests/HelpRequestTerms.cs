namespace Barnabas.Domain.Requests;

/// <summary>
/// The terms a Help request carries: which of the offer's declared windows the requester wants.
/// </summary>
/// <remarks>
/// A window rather than a time. Help offers time the owner actually has, so a requester chooses
/// among what was offered instead of naming an hour of their own - which is why this holds the
/// window's identifier and not a moment.
/// </remarks>
public sealed record HelpRequestTerms(Guid AvailabilityWindowId);
