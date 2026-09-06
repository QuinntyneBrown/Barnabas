using Barnabas.Application.Requests.MakeLoanRequest;
using MediatR;

namespace Barnabas.Application.Requests.MakeHelpRequest;

/// <summary>
/// Asks for help in one of the windows the offer declared.
/// </summary>
/// <remarks>
/// The only kind whose request carries no pickup. Help offers time, so there is no object to
/// collect; what the requester chooses is one of the windows the owner said they were free -
/// <c>L2-057</c>.
/// </remarks>
public sealed record MakeHelpRequestCommand(
    Guid ListingId,
    string Message,
    Guid? AvailabilityWindowId) : IRequest<MadeRequestResult>;
