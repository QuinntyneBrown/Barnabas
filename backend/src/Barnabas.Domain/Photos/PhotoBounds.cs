namespace Barnabas.Domain.Photos;

/// <summary>
/// How large each size may be, and how large an upload may be before it is decoded.
/// </summary>
/// <remarks>
/// The pixel bound is separate from the byte bound and neither replaces the other. A 12 MB file
/// is refused by its length; a 400 KB file declaring a 30000 x 30000 canvas is refused by
/// <see cref="MaxPixels"/>, because decoding it would allocate 3.6 GB and the byte gate would
/// never have seen it coming.
/// </remarks>
public static class PhotoBounds
{
    /// <summary>The largest upload the API accepts, in bytes. L2-032 AC1 takes 2 MB; AC2 refuses 12.</summary>
    public const long MaxUploadBytes = 10L * 1024 * 1024;

    /// <summary>The most pixels a decoded image may hold, before it is decoded.</summary>
    public const long MaxPixels = 50_000_000;

    /// <summary>The longest edge each size is bounded to.</summary>
    public static int LongestEdge(PhotoSize size) => size switch
    {
        PhotoSize.Full => 1600,
        PhotoSize.Board => 640,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown photo size."),
    };
}
