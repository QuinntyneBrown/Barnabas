namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member fills in to ask to borrow something.
/// </summary>
/// <remarks>
/// The listing is a route value rather than a field, so the thing being asked for is part of the
/// address of the ask.
/// </remarks>
public sealed record MakeLoanRequestRequest(
    string Message,
    DateOnly? PickupOn,
    DateOnly? ReturnBy,
    bool LoanAcknowledged);
