using SkiaSharp;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Real image bytes, made rather than checked in.
/// </summary>
/// <remarks>
/// A binary fixture in the repository would be a file nobody could read in a diff and nobody
/// could adjust — and several of these tests need a particular size, which a fixed file cannot
/// give. These are generated, so a two-megabyte JPEG is two megabytes because the test asked for
/// two megabytes.
/// <para>
/// Noise rather than a flat colour. A solid field compresses to almost nothing, so a JPEG of a
/// requested size could not be produced from one at all.
/// </para>
/// </remarks>
public static class Images
{
    /// <summary>A valid JPEG of roughly the requested length, never shorter.</summary>
    public static byte[] Jpeg(int approximateBytes)
    {
        // Grown until it passes the requested length. Compression is not linear in the pixel
        // count, so guessing dimensions once and hoping would produce a file of any size but the
        // one asked for.
        var edge = 256;

        while (edge < 8_000)
        {
            var bytes = Encode(Noise(edge, edge), SKEncodedImageFormat.Jpeg);

            if (bytes.Length >= approximateBytes)
            {
                return bytes;
            }

            edge *= 2;
        }

        throw new InvalidOperationException($"Could not make a JPEG of {approximateBytes} bytes.");
    }

    public static byte[] Png() => Encode(Noise(64, 64), SKEncodedImageFormat.Png);

    /// <summary>
    /// A JPEG carrying an APP1 segment, standing in for an EXIF block.
    /// </summary>
    /// <remarks>
    /// Written by hand rather than by an imaging library, because the point is only that
    /// something recognisable is embedded in the file and is absent from what comes back. A real
    /// EXIF block would be a more elaborate way of asserting the same thing.
    /// <para>
    /// It goes immediately after the two-byte SOI marker, which is where a decoder expects
    /// application segments and where a decoder that ignores them will skip it from.
    /// </para>
    /// </remarks>
    public static byte[] JpegWithExif(string marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        var jpeg = Jpeg(40_000);
        var payload = System.Text.Encoding.ASCII.GetBytes("Exif\0\0" + marker);

        // Length counts itself and the payload, and is big-endian.
        var length = payload.Length + 2;

        var segment = new byte[4 + payload.Length];

        segment[0] = 0xFF;
        segment[1] = 0xE1;
        segment[2] = (byte)(length >> 8);
        segment[3] = (byte)(length & 0xFF);

        payload.CopyTo(segment, 4);

        return [.. jpeg[..2], .. segment, .. jpeg[2..]];
    }

    private static SKBitmap Noise(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);

        // A fixed seed, so a failure is the same failure the next time it is run.
        var random = new Random(20260906);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256)));
            }
        }

        return bitmap;
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format)
    {
        using (bitmap)
        using (var image = SKImage.FromBitmap(bitmap))
        using (var data = image.Encode(format, 100))
        {
            return data.ToArray();
        }
    }
}
