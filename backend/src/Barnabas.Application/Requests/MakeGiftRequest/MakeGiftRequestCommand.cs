using Barnabas.Application.Requests.MakeLoanRequest;
using MediatR;

namespace Barnabas.Application.Requests.MakeGiftRequest;

/// <summary>
/// Asks for a Give listing.
/// </summary>
/// <remarks>
/// A message and a proposed pickup time, and nothing else. There is no return date because
/// ownership transfers, and no price because a gift has none - <c>L2-055</c>.
/// </remarks>
public sealed record MakeGiftRequestCommand(
    Guid ListingId,
    string Message,
    DateTimeOffset? PickupAt) : IRequest<MadeRequestResult>;
