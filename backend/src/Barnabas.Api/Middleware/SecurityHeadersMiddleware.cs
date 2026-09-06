using System.Globalization;
using Microsoft.Extensions.Options;

namespace Barnabas.Api.Middleware;

/// <summary>
/// Puts the same security headers on every response, whatever produced it.
/// </summary>
/// <remarks>
/// Here rather than on a filter or a base controller, so an error page, a health check and a
/// photo carry them as surely as a controller action does — the responses most likely to be
/// forgotten are exactly the ones no controller wrote.
/// <para>
/// <c>Strict-Transport-Security</c> is emitted rather than left to <c>UseHsts</c>, which skips
/// any request that did not arrive over HTTPS. Behind a terminating proxy that is every request,
/// so the built-in middleware would quietly emit nothing in exactly the deployment shape this
/// is meant for — and <c>L2-100 AC1</c> asks for the header.
/// </para>
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeaderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Registered on the response rather than written now: the headers have to be on the
        // response that is actually sent, including one an exception handler produced.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

            // Refuses the browser's own guess at a content type. Without it, a stored file the
            // API says is an image could still be executed as script by a browser that thought
            // it knew better - L2-102 AC3.
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "DENY";

            headers["Strict-Transport-Security"] = string.Create(
                CultureInfo.InvariantCulture,
                $"max-age={_options.StrictTransportSecuritySeconds}; includeSubDomains");

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
