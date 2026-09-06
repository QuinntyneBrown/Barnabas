namespace Barnabas.Api.Middleware;

/// <summary>What the security headers say, and whether transport security is enforced.</summary>
public sealed class SecurityHeaderOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// The policy the browser is handed.
    /// </summary>
    /// <remarks>
    /// No <c>'unsafe-inline'</c> in <c>script-src</c>, which is the half of <c>L2-097 AC3</c>
    /// that matters: a policy that allowed inline script would be present and useless. Angular
    /// emits no inline script, so nothing here is a concession to the build.
    /// <para>
    /// Styles do allow inline, because the framework writes them for animations and component
    /// scoping. That is a real weakening and it is stated rather than hidden; it does not permit
    /// execution.
    /// </para>
    /// </remarks>
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    /// <summary>
    /// Whether plain HTTP is redirected to HTTPS.
    /// </summary>
    /// <remarks>
    /// On everywhere but the two suites, which drive the API over plain HTTP through a test
    /// server and a dev proxy. The one acceptance test that asserts the redirect turns it back
    /// on for itself, so the requirement is tested rather than assumed.
    /// </remarks>
    public bool RequireHttps { get; set; } = true;

    /// <summary>How long a browser should refuse to speak plain HTTP to this host, in seconds.</summary>
    public int StrictTransportSecuritySeconds { get; set; } = 31_536_000;
}
