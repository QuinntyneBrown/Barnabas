using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Photos.AttachPhoto;

/// <summary>
/// Renders the upload, stores the renditions, and points the listing at them.
/// </summary>
/// <remarks>
/// The order matters. The image is rendered <em>first</em>, so an upload that is not an image
/// fails before anything is written; the bytes go to the store next; the row is saved last. A
/// failure at the last step leaves orphaned bytes rather than a listing pointing at nothing, and
/// of the two that is the one nobody sees.
/// <para>
/// A replaced photo's bytes are deleted after the save rather than before. Deleting first would
/// mean a save that failed had already destroyed the picture that was there.
/// </para>
/// </remarks>
public sealed class AttachPhotoCommandHandler : IRequestHandler<AttachPhotoCommand, AttachedPhotoResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly IImageProcessor _images;
    private readonly IPhotoStore _photos;

    public AttachPhotoCommandHandler(
        IBarnabasDbContext context,
        IImageProcessor images,
        IPhotoStore photos)
    {
        _context = context;
        _images = images;
        _photos = photos;
    }

    public async Task<AttachedPhotoResult> Handle(
        AttachPhotoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        var rendered = _images.Render(request.Bytes, request.ContentType);

        var photoId = Guid.NewGuid();

        foreach (var (size, photo) in rendered)
        {
            await _photos.SaveAsync(photoId, size, photo, cancellationToken);
        }

        var displaced = listing.AttachPhoto(photoId);

        await _context.SaveChangesAsync(cancellationToken);

        if (displaced is { } previous)
        {
            await _photos.DeleteAsync(previous, cancellationToken);
        }

        return new AttachedPhotoResult(
            listing.Id,
            photoId,
            PhotoUrl.For(photoId, PhotoSize.Full),
            PhotoUrl.For(photoId, PhotoSize.Board));
    }
}
