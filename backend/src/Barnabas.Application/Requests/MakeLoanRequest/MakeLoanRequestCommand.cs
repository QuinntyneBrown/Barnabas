using MediatR;

namespace Barnabas.Application.Requests.MakeLoanRequest;

/// <summary>
/// Asks to borrow a Lend listing.
/// </summary>
/// <remarks>
/// One command per kind rather than one carrying a discriminator. The four kinds collect
/// different terms, and a single command would branch on kind at every step - which is the shape
/// that let the kinds blur into one form before.
/// </remarks>
public sealed record MakeLoanRequestCommand(
    Guid ListingId,
    string Message,
    DateOnly? PickupOn,
    DateOnly? ReturnBy,
    bool LoanAcknowledged) : IRequest<MadeRequestResult>;
