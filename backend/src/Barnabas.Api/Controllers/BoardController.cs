using Barnabas.Application.Board.GetBoard;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>The main feed: everything the congregation currently has on offer.</summary>
[ApiController]
[Route("board")]
public sealed class BoardController : ControllerBase
{
    /// <summary>Enough for the longest board a parish is likely to post, in one request.</summary>
    private const int DefaultLimit = 24;

    private readonly ISender _sender;

    public BoardController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<BoardPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardPage>> Get(
        [FromQuery] ListingKind? kind,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetBoardQuery(kind, limit ?? DefaultLimit, cursor), cancellationToken));
}
