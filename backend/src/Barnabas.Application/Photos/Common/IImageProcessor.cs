using Barnabas.Domain.Photos;

namespace Barnabas.Application.Photos.Common;

/// <summary>
/// Turns whatever was uploaded into the renditions Barnabas keeps.
/// </summary>
/// <remarks>
/// The uploaded bytes are never stored. Every rendition is decoded and re-encoded, which is what
/// <c>L2-102 AC2</c> asks for and is also what removes the embedded metadata — a re-encode writes
/// a new file from pixels, so there is no EXIF block to strip because none is carried over.
/// <para>
/// It is an interface in the application layer because deciding <em>that</em> an upload is
/// re-encoded is a rule, and deciding <em>how</em> is infrastructure.
/// </para>
/// </remarks>
public interface IImageProcessor
{
    /// <summary>
    /// Decodes, bounds and re-encodes, or refuses.
    /// </summary>
    /// <exception cref="UnsupportedImageException">
    /// The bytes are not a permitted image, do not match the type that was declared, or declare a
    /// canvas too large to decode.
    /// </exception>
    IReadOnlyDictionary<PhotoSize, StoredPhoto> Render(byte[] uploaded, string declaredContentType);
}
