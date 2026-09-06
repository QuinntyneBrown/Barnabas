namespace Barnabas.Domain.Photos;

/// <summary>
/// Raised when what was uploaded is not an image of a type Barnabas keeps.
/// </summary>
/// <remarks>
/// One exception for every way of failing that test — a declared type that does not match the
/// bytes, bytes that decode to nothing, a canvas too large to decode. Saying which would tell
/// somebody probing the endpoint how close they got.
/// </remarks>
public sealed class UnsupportedImageException : Exception
{
    public UnsupportedImageException()
        : base("That file is not an image Barnabas can accept.")
    {
    }
}
