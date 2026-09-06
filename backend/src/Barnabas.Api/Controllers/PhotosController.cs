using Barnabas.Api.Middleware;
using Barnabas.Application.Photos.AttachPhoto;
using Barnabas.Application.Photos.GetPhoto;
using Barnabas.Domain.Photos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Putting a photo on a listing, and fetching one.
/// </summary>
/// <remarks>
/// The two halves face opposite ways, as they do for joining. Attaching demands the listing's
/// owner and carries a body ten times the size anything else may; fetching is anonymous, because
/// a browser loading an <c>&lt;img&gt;</c> cannot attach a bearer token and the alternatives cost
/// more than they buy. What stands in for the session there is the identifier: a version-4 GUID
/// nobody can enumerate, disclosed only inside a congregation's own responses.
/// <para>
/// Every response says what it is and refuses to be sniffed into anything else, and none of them
/// is served as static content — which is <c>L2-102 AC3</c>.
/// </para>
/// </remarks>
[ApiController]
public sealed class PhotosController : ControllerBase
{
    private readonly ISender _sender;

    public PhotosController(ISender sender) => _sender = sender;

    /// <summary>
    /// Attaches one photo, replacing whatever was there.
    /// </summary>
    /// <remarks>
    /// The body limit is raised on this action alone. Everything else in the API stops at 1 MB —
    /// <c>L2-096 AC2</c> — and a 12 MB upload is still refused with 413 by the same middleware
    /// reading the higher number.
    /// </remarks>
    [HttpPost("listings/{listingId:guid}/photo")]
    [MaxRequestBody(PhotoBounds.MaxUploadBytes)]
    [ProducesResponseType<AttachedPhotoResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<AttachedPhotoResult>> Attach(
        Guid listingId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["Choose a photo to attach."] })
            {
                Title = "One or more fields are invalid.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        // Read into memory rather than streamed to the processor, because the decoder needs the
        // whole file to sniff it and the length is already bounded to 10 MB by the time we are here.
        using var buffer = new MemoryStream();

        await file.CopyToAsync(buffer, cancellationToken);

        var attached = await _sender.Send(
            new AttachPhotoCommand(listingId, buffer.ToArray(), file.ContentType),
            cancellationToken);

        return Created(attached.Url, attached);
    }

    [AllowAnonymous]
    [HttpGet("photos/{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> Full(Guid photoId, CancellationToken cancellationToken) =>
        ServeAsync(photoId, PhotoSize.Full, cancellationToken);

    /// <summary>The board's rendition, which is what a placard asks for. L2-106 AC1.</summary>
    [AllowAnonymous]
    [HttpGet("photos/{photoId:guid}/board")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> Board(Guid photoId, CancellationToken cancellationToken) =>
        ServeAsync(photoId, PhotoSize.Board, cancellationToken);

    private async Task<ActionResult> ServeAsync(
        Guid photoId,
        PhotoSize size,
        CancellationToken cancellationToken)
    {
        var photo = await _sender.Send(new GetPhotoQuery(photoId, size), cancellationToken);

        // Private, not public. The identifier is a capability, and a shared cache holding a
        // congregation's photographs is not something anybody agreed to.
        Response.Headers.CacheControl = "private, max-age=3600";

        // Inline and named, so nothing downstream has to guess what it is holding.
        Response.Headers.ContentDisposition = $"inline; filename=\"{photoId:n}.jpg\"";

        return File(photo.Bytes, photo.ContentType);
    }
}
