using Barnabas.Api.Contracts;
using Barnabas.Application.Messaging.GetMyThreads;
using Barnabas.Application.Messaging.GetThread;
using Barnabas.Application.Messaging.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Where a handoff is arranged.
/// </summary>
/// <remarks>
/// There is no action here that creates a thread. One exists only as a consequence of an
/// accepted request, which is what guarantees every conversation in Barnabas has a subject.
/// </remarks>
[ApiController]
[Route("threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly ISender _sender;

    public ThreadsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ThreadSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ThreadSummaryDto>>> Mine(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMyThreadsQuery(), cancellationToken));

    [HttpGet("{threadId:guid}")]
    [ProducesResponseType<ThreadDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThreadDetailDto>> Get(Guid threadId, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetThreadQuery(threadId), cancellationToken));

    [HttpPost("{threadId:guid}/messages")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> Send(
        Guid threadId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = await _sender.Send(new SendMessageCommand(threadId, request.Body), cancellationToken);

        return Created($"/threads/{threadId}", message);
    }
}
