namespace Barnabas.Infrastructure.Photos;

/// <summary>Where the photo bytes are kept.</summary>
public sealed class PhotoStoreOptions
{
    public const string SectionName = "Photos";

    /// <summary>
    /// The folder the renditions are written under.
    /// </summary>
    /// <remarks>
    /// Outside the content root by default. A folder the web server serves from is a folder an
    /// uploaded file could be executed from, and <c>L2-102 AC3</c> asks that it cannot be —
    /// nothing here is ever mapped as static content, and every byte leaves through an endpoint
    /// that names its own content type.
    /// </remarks>
    public string Root { get; set; } = DefaultRoot;

    /// <summary>Where the bytes go when a deployment has not said.</summary>
    public static string DefaultRoot { get; } =
        Path.Combine(Path.GetTempPath(), "barnabas", "photos");
}
