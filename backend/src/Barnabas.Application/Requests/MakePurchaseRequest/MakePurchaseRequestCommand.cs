using Barnabas.Application.Requests.MakeLoanRequest;
using MediatR;

namespace Barnabas.Application.Requests.MakePurchaseRequest;

/// <summary>
/// Asks to buy a Sell listing.
/// </summary>
/// <remarks>
/// The same two fields a gift request collects, and for the same reason: what a buyer and a
/// seller need from Barnabas is to agree when to meet. The price is on the listing and is
/// settled between them in person, so no payment field is accepted here - <c>L2-056</c>,
/// <c>L2-063</c>.
/// </remarks>
public sealed record MakePurchaseRequestCommand(
    Guid ListingId,
    string Message,
    DateTimeOffset? PickupAt) : IRequest<MadeRequestResult>;
