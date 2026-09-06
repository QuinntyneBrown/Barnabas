using Barnabas.Api.Contracts;
using Barnabas.Application.Listings.ArchiveListing;
using Barnabas.Application.Listings.CloseOutListing;
using Barnabas.Application.Listings.DeleteListing;
using Barnabas.Application.Listings.EditListing;
using Barnabas.Application.Listings.GetListing;
using Barnabas.Application.Listings.GetMyListings;
using Barnabas.Application.Listings.PostGiveListing;
using Barnabas.Application.Listings.PostHelpListing;
using Barnabas.Application.Listings.PostLendListing;
using Barnabas.Application.Listings.PostSellListing;
using Barnabas.Application.Listings.RestoreListing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>Posting, reading, and closing out listings.</summary>
/// <remarks>
/// One route per kind rather than one route taking a kind. The four kinds collect different
/// fields and refuse different ones, so a single endpoint would have to branch on kind at every
/// step - which is the shape that let a gift acquire a price in an earlier iteration of this
/// product. Separate routes also give <c>L2-026</c> its meaning: the kind is chosen before any
/// detail is entered, and it is the route, so it cannot be silently defaulted.
/// </remarks>
[ApiController]
[Route("listings")]
public sealed class ListingsController : ControllerBase
{
    private readonly ISender _sender;

    public ListingsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Refuses a listing that names no kind.
    /// </summary>
    /// <remarks>
    /// The four kinds each have a route, so the kind is chosen before any detail is entered and
    /// cannot be defaulted. This endpoint exists so that a caller who posts to the collection
    /// gets told which field is missing rather than a bare 405, which would say nothing about
    /// what to do next. It holds no logic and dispatches nothing: there is no listing here to
    /// create, only a choice that has not been made. <c>L2-026</c>.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult PostWithoutAKind()
    {
        ModelState.AddModelError(
            "kind",
            "Choose whether you are lending, giving, selling, or offering help, and post to that kind.");

        return ValidationProblem(ModelState);
    }

    [HttpPost("lend")]
    [ProducesResponseType<PostedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostedListingResult>> PostLend(
        PostLendListingCommand command,
        CancellationToken cancellationToken) =>
        await PostedAsync(command, cancellationToken);

    [HttpPost("give")]
    [ProducesResponseType<PostedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostedListingResult>> PostGive(
        PostGiveListingCommand command,
        CancellationToken cancellationToken) =>
        await PostedAsync(command, cancellationToken);

    [HttpPost("sell")]
    [ProducesResponseType<PostedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostedListingResult>> PostSell(
        PostSellListingCommand command,
        CancellationToken cancellationToken) =>
        await PostedAsync(command, cancellationToken);

    [HttpPost("help")]
    [ProducesResponseType<PostedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostedListingResult>> PostHelp(
        PostHelpListingCommand command,
        CancellationToken cancellationToken) =>
        await PostedAsync(command, cancellationToken);

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

    /// <summary>Corrects a listing's details. Not its kind.</summary>
    [HttpPut("{listingId:guid}")]
    [ProducesResponseType<EditedListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EditedListingResult>> Edit(
        Guid listingId,
        EditListingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new EditListingCommand(
                listingId,
                request.Title,
                request.Description,
                request.Category,
                request.Neighbourhood),
            cancellationToken));
    }

    /// <summary>Takes it off the board, keeping it recoverable.</summary>
    [HttpPost("{listingId:guid}/archive")]
    [ProducesResponseType<ArchivedListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArchivedListingResult>> Archive(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ArchiveListingCommand(listingId), cancellationToken));

    /// <summary>Puts a shelved listing back. A closed-out one stays where it is.</summary>
    [HttpPost("{listingId:guid}/restore")]
    [ProducesResponseType<RestoredListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RestoredListingResult>> Restore(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new RestoreListingCommand(listingId), cancellationToken));

    /// <summary>Removes it for good, along with the conversations it opened.</summary>
    [HttpDelete("{listingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(Guid listingId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteListingCommand(listingId), cancellationToken);

        return NoContent();
    }

    [HttpPost("{listingId:guid}/close-out")]
    [ProducesResponseType<CloseOutListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CloseOutListingResult>> CloseOut(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new CloseOutListingCommand(listingId), cancellationToken));

    /// <summary>
    /// Dispatches a post command and answers 201 at the new listing.
    /// </summary>
    /// <remarks>
    /// Shared by the four kinds because what happens after a listing is created is the same for
    /// all of them. What differs between the kinds is the command, and that difference is
    /// already carried by the four routes.
    /// </remarks>
    private async Task<ActionResult<PostedListingResult>> PostedAsync(
        IRequest<PostedListingResult> command,
        CancellationToken cancellationToken)
    {
        var posted = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { listingId = posted.ListingId }, posted);
    }
}
