using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using SkiaSharp;

namespace Barnabas.Infrastructure.Photos;

/// <summary>
/// Decodes an upload, bounds it, and writes fresh JPEGs from the pixels.
/// </summary>
/// <remarks>
/// Three separate refusals, in an order that matters.
/// <list type="number">
/// <item>
/// The bytes are sniffed and compared with the type the client declared. A PHP script named
/// <c>.jpg</c> and a GIF sent as <c>image/jpeg</c> are both refused here, before anything decodes
/// them — <c>L2-102 AC1</c>.
/// </item>
/// <item>
/// The dimensions are read from the header before a pixel is allocated. A 400 KB file may declare
/// a 30000 x 30000 canvas, which decodes to 3.6 GB; the byte-length gate would never have seen it
/// coming, so <see cref="PhotoBounds.MaxPixels"/> is checked against what the header claims.
/// </item>
/// <item>
/// Only then is it decoded. A file that passes both and still decodes to nothing is refused with
/// the same message as the other two, because saying which would tell somebody probing the
/// endpoint how close they got.
/// </item>
/// </list>
/// Every rendition is re-encoded from the decoded pixels, which is what <c>L2-102 AC2</c> asks
/// for. The metadata is not stripped so much as never carried: an encode writes a new file from a
/// bitmap, and a bitmap holds no EXIF block, no GPS tag and no colour profile to bring along.
/// </remarks>
public sealed class SkiaImageProcessor : IImageProcessor
{
    private const int JpegQuality = 82;

    /// <summary>What Barnabas accepts, and the first bytes each of them actually begins with.</summary>
    private static readonly (string ContentType, byte[] Magic)[] Permitted =
    [
        ("image/jpeg", [0xFF, 0xD8, 0xFF]),
        ("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        ("image/webp", [0x52, 0x49, 0x46, 0x46]),
    ];

    public IReadOnlyDictionary<PhotoSize, StoredPhoto> Render(byte[] uploaded, string declaredContentType)
    {
        ArgumentNullException.ThrowIfNull(uploaded);

        var sniffed = Sniff(uploaded);

        if (sniffed is null || !Declares(declaredContentType, sniffed))
        {
            throw new UnsupportedImageException();
        }

        using var data = SKData.CreateCopy(uploaded);
        using var codec = SKCodec.Create(data);

        if (codec is null)
        {
            throw new UnsupportedImageException();
        }

        var info = codec.Info;

        if ((long)info.Width * info.Height > PhotoBounds.MaxPixels || info.Width <= 0 || info.Height <= 0)
        {
            throw new UnsupportedImageException();
        }

        using var decoded = SKBitmap.Decode(codec);

        if (decoded is null)
        {
            throw new UnsupportedImageException();
        }

        return new Dictionary<PhotoSize, StoredPhoto>
        {
            [PhotoSize.Full] = Encode(decoded, PhotoSize.Full),
            [PhotoSize.Board] = Encode(decoded, PhotoSize.Board),
        };
    }

    /// <summary>
    /// One rendition, bounded by its longest edge and never enlarged.
    /// </summary>
    /// <remarks>
    /// Upscaling a small photo to fill a bound would make a bigger file that looks worse. A photo
    /// already inside the bound is re-encoded at its own size, which still re-encodes it — the
    /// point of the exercise is the re-encode, not the resize.
    /// </remarks>
    private static StoredPhoto Encode(SKBitmap source, PhotoSize size)
    {
        var bound = PhotoBounds.LongestEdge(size);
        var longest = Math.Max(source.Width, source.Height);
        var scale = longest <= bound ? 1d : (double)bound / longest;

        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        using var resized = source.Resize(
            new SKImageInfo(width, height),
            new SKSamplingOptions(SKCubicResampler.Mitchell));

        using var image = SKImage.FromBitmap(resized ?? source);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality)
            ?? throw new UnsupportedImageException();

        return new StoredPhoto(encoded.ToArray(), "image/jpeg");
    }

    /// <summary>What the bytes actually are, or null when they are nothing Barnabas keeps.</summary>
    private static string? Sniff(byte[] bytes)
    {
        foreach (var (contentType, magic) in Permitted)
        {
            if (bytes.Length < magic.Length || !bytes.AsSpan(0, magic.Length).SequenceEqual(magic))
            {
                continue;
            }

            // RIFF is a container, not a format. WEBP is what says it holds an image.
            if (contentType == "image/webp"
                && (bytes.Length < 12 || !bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)))
            {
                continue;
            }

            return contentType;
        }

        return null;
    }

    /// <summary>
    /// Whether the declared type matches what the bytes are.
    /// </summary>
    /// <remarks>
    /// A declared type nobody sent — a browser posting <c>application/octet-stream</c>, or a form
    /// part with no type at all — is not a mismatch, because nothing was claimed. What
    /// <c>L2-102 AC1</c> refuses is a claim that is wrong.
    /// </remarks>
    private static bool Declares(string declaredContentType, string sniffed)
    {
        var declared = declaredContentType?.Split(';')[0].Trim();

        if (string.IsNullOrEmpty(declared) || declared == "application/octet-stream")
        {
            return true;
        }

        return string.Equals(declared, sniffed, StringComparison.OrdinalIgnoreCase);
    }
}
