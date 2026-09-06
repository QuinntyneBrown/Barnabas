using Barnabas.Domain.Photos;

namespace Barnabas.Application.Photos.Common;

/// <summary>
/// Where a photo is fetched from.
/// </summary>
/// <remarks>
/// Built in one place rather than assembled in each projection, because it appears on the board,
/// on the listing screen and in a member's data export, and three spellings of the same path is
/// three chances for one of them to be wrong.
/// <para>
/// It is a path and not an absolute URL. Which host serves it is the deployment's business, and a
/// stored absolute URL would be wrong the first time one moved.
/// </para>
/// </remarks>
public static class PhotoUrl
{
    public static string For(Guid photoId, PhotoSize size) => size switch
    {
        PhotoSize.Full => $"/photos/{photoId}",
        PhotoSize.Board => $"/photos/{photoId}/board",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown photo size."),
    };
}
