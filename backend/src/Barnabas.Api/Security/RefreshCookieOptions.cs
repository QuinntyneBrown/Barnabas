namespace Barnabas.Api.Security;

/// <summary>
/// How the refresh token travels.
/// </summary>
/// <remarks>
/// It is a cookie rather than a field in the response body, and an HttpOnly one, so that script
/// on the page cannot read it. The access token is short-lived and is held in memory by the web
/// client; the refresh token is the long-lived secret, and it is the one worth putting out of
/// reach of any cross-site scripting that ever gets through.
/// <para>
/// The path is the root rather than <c>/sessions</c>, because the browser sees the web client's
/// proxy prefix and a narrower path would simply never be sent back.
/// </para>
/// </remarks>
public sealed class RefreshCookieOptions
{
    public const string SectionName = "Auth:RefreshCookie";

    public const string CookieName = "barnabas_rt";

    /// <summary>Off in development only, where the dev server speaks plain HTTP.</summary>
    public bool Secure { get; set; } = true;
}
