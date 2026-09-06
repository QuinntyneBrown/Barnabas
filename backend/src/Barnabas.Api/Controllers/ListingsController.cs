using Barnabas.Application.Listings.CloseOutListing;
using Barnabas.Application.Listings.GetListing;
using Barnabas.Application.Listings.GetMyListings;
using Barnabas.Application.Listings.PostLendListing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>Posting, reading, and closing out listings.</summary>
/// <remarks>
/// Only the Lend kind can be posted in feature slice 1. Give, Sell, and Help are each one more
/// command, handler, validator, and form against the architecture this settles.
/// </remarks>
[ApiController]
[Route("listings")]
public sealed class ListingsController : ControllerBase
{
    private readonly ISender _sender;

    public ListingsController(ISender sender) => _sender = sender;

    [HttpPost("lend")]
    [ProducesResponseType<PostedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostedListingResult>> PostLend(
        PostLendListingCommand command,
        CancellationToken cancellationToken)
    {
        var posted = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { listingId = posted.ListingId }, posted);
    }

    /// <summary>
    /// The caller's own listings.
    /// </summary>
    /// <remarks>
    /// Routed before <c>{listingId}</c> so that the literal segment wins. Nothing else would
    /// stop "mine" being read as an identifier and answering 400 for a malformed GUID.
    /// </remarks>
    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<MyListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyListingDto>>> Mine(
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMyListingsQuery(includeClosed), cancellationToken));

    [HttpGet("{listingId:guid}")]
    [ProducesResponseType<ListingDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingDetailDto>> Get(Guid listingId, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetListingQuery(listingId), cancellationToken));

    [HttpPost("{listingId:guid}/close-out")]
    [ProducesResponseType<CloseOutListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CloseOutListingResult>> CloseOut(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new CloseOutListingCommand(listingId), cancellationToken));
}
