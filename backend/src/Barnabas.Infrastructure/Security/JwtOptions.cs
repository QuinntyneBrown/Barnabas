namespace Barnabas.Infrastructure.Security;

/// <summary>How access tokens are signed and how long they last.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "barnabas";

    public string Audience { get; set; } = "barnabas";

    /// <summary>Symmetric signing key. Supplied by configuration; never defaulted in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Deliberately short. A leaked access token is useful only briefly, and the refresh token
    /// is what spares the member another trip to their inbox.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
