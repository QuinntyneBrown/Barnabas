using Barnabas.Api.Contracts;
using Barnabas.Application.Requests.AcceptRequest;
using Barnabas.Application.Requests.DeclineRequest;
using Barnabas.Application.Requests.GetIncomingRequests;
using Barnabas.Application.Requests.GetMyRequests;
using Barnabas.Application.Requests.MakeGiftRequest;
using Barnabas.Application.Requests.MakeHelpRequest;
using Barnabas.Application.Requests.MakeLoanRequest;
using Barnabas.Application.Requests.MakePurchaseRequest;
using Barnabas.Domain.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Asking for a listing, and deciding what was asked.
/// </summary>
/// <remarks>
/// Making a request is routed under the listing it concerns; deciding one is routed under the
/// request. One endpoint per kind, because the four collect different terms and refuse different
/// fields - and because the endpoint is what ties a request to the kind of listing it is against,
/// which is the check that stops a Sell listing acquiring a request carrying loan terms.
/// </remarks>
[ApiController]
public sealed class RequestsController : ControllerBase
{
    private readonly ISender _sender;

    public RequestsController(ISender sender) => _sender = sender;

    [HttpPost("listings/{listingId:guid}/requests/loan")]
    [ProducesResponseType<MadeRequestResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MadeRequestResult>> MakeLoanRequest(
        Guid listingId,
        MakeLoanRequestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var made = await _sender.Send(
            new MakeLoanRequestCommand(
                listingId,
                request.Message,
                request.PickupOn,
                request.ReturnBy,
                request.LoanAcknowledged),
            cancellationToken);

        return Created($"/requests/{made.RequestId}", made);
    }

    [HttpPost("listings/{listingId:guid}/requests/gift")]
    [ProducesResponseType<MadeRequestResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MadeRequestResult>> MakeGiftRequest(
        Guid listingId,
        MakeGiftRequestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var made = await _sender.Send(
            new MakeGiftRequestCommand(listingId, request.Message, request.PickupAt),
            cancellationToken);

        return Created($"/requests/{made.RequestId}", made);
    }

    [HttpPost("listings/{listingId:guid}/requests/purchase")]
    [ProducesResponseType<MadeRequestResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MadeRequestResult>> MakePurchaseRequest(
        Guid listingId,
        MakePurchaseRequestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var made = await _sender.Send(
            new MakePurchaseRequestCommand(listingId, request.Message, request.PickupAt),
            cancellationToken);

        return Created($"/requests/{made.RequestId}", made);
    }

    [HttpPost("listings/{listingId:guid}/requests/help")]
    [ProducesResponseType<MadeRequestResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MadeRequestResult>> MakeHelpRequest(
        Guid listingId,
        MakeHelpRequestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var made = await _sender.Send(
            new MakeHelpRequestCommand(listingId, request.Message, request.AvailabilityWindowId),
            cancellationToken);

        return Created($"/requests/{made.RequestId}", made);
    }

    [HttpGet("requests/incoming")]
    [ProducesResponseType<IReadOnlyList<IncomingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncomingRequestDto>>> Incoming(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetIncomingRequestsQuery(), cancellationToken));

    [HttpGet("requests/mine")]
    [ProducesResponseType<IReadOnlyList<MyRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyRequestDto>>> Mine(
        [FromQuery] RequestStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMyRequestsQuery(status), cancellationToken));

    [HttpPost("requests/{requestId:guid}/accept")]
    [ProducesResponseType<AcceptRequestResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AcceptRequestResult>> Accept(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new AcceptRequestCommand(requestId), cancellationToken));

    [HttpPost("requests/{requestId:guid}/decline")]
    [ProducesResponseType<DeclineRequestResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeclineRequestResult>> Decline(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new DeclineRequestCommand(requestId), cancellationToken));
}
