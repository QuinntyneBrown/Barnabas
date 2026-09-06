using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using Microsoft.Extensions.Options;

namespace Barnabas.Infrastructure.Photos;

/// <summary>
/// Keeps the renditions as files, one per photo per size.
/// </summary>
/// <remarks>
/// A folder rather than a table, because image bytes in a row make every query that touches the
/// table slower and every backup larger, and nothing about a photo needs a transaction with the
/// listing.
/// <para>
/// A deployment will want object storage instead. That is why this sits behind
/// <see cref="IPhotoStore"/> and why no handler names it.
/// </para>
/// <para>
/// The file name is built from a <see cref="Guid"/> and an enum, never from anything a caller
/// typed, so there is no path for a traversal to be attempted through — the identifier is parsed
/// as a GUID by the route before it reaches here, and <see cref="Guid.ToString(string)"/> emits
/// nothing but hex.
/// </para>
/// </remarks>
public sealed class FileSystemPhotoStore : IPhotoStore
{
    private readonly string _root;

    public FileSystemPhotoStore(IOptions<PhotoStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // A blank setting means "wherever the default is", not "the current directory". Left to
        // Path.Combine, an empty root would quietly write the photos beside the executable.
        _root = string.IsNullOrWhiteSpace(options.Value.Root)
            ? PhotoStoreOptions.DefaultRoot
            : options.Value.Root;
    }

    public async Task SaveAsync(
        Guid photoId,
        PhotoSize size,
        StoredPhoto photo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(photo);

        Directory.CreateDirectory(_root);

        await File.WriteAllBytesAsync(PathFor(photoId, size), photo.Bytes, cancellationToken);
    }

    public async Task<StoredPhoto?> ReadAsync(
        Guid photoId,
        PhotoSize size,
        CancellationToken cancellationToken)
    {
        var path = PathFor(photoId, size);

        if (!File.Exists(path))
        {
            return null;
        }

        // Every rendition is written as a JPEG by the processor, so the type is a property of the
        // store rather than something read back off the file.
        return new StoredPhoto(await File.ReadAllBytesAsync(path, cancellationToken), "image/jpeg");
    }

    public Task DeleteAsync(Guid photoId, CancellationToken cancellationToken)
    {
        foreach (var size in Enum.GetValues<PhotoSize>())
        {
            var path = PathFor(photoId, size);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }

    private string PathFor(Guid photoId, PhotoSize size) =>
        Path.Combine(_root, $"{photoId:n}-{size.ToString().ToLowerInvariant()}.jpg");
}
