using Barnabas.Domain.Photos;

namespace Barnabas.Application.Photos.Common;

/// <summary>
/// Where the bytes of a photo live.
/// </summary>
/// <remarks>
/// Not the database. Image bytes in a row make every query that touches the table slower and
/// every backup larger, and nothing about a photo needs a transaction with the listing — a
/// listing that saved and a photo that did not is a listing without a picture, which is a state
/// the product already has a name for.
/// <para>
/// An interface because a deployment will want object storage and a developer machine wants a
/// folder. Neither belongs in a handler.
/// </para>
/// </remarks>
public interface IPhotoStore
{
    Task SaveAsync(Guid photoId, PhotoSize size, StoredPhoto photo, CancellationToken cancellationToken);

    Task<StoredPhoto?> ReadAsync(Guid photoId, PhotoSize size, CancellationToken cancellationToken);

    /// <summary>Removes every rendition. Absent bytes are not an error — deletion is idempotent.</summary>
    Task DeleteAsync(Guid photoId, CancellationToken cancellationToken);
}
