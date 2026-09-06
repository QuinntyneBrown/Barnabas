using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Middleware;

/// <summary>
/// Refuses a body larger than the API accepts, before anything tries to read it.
/// </summary>
/// <remarks>
/// Kestrel enforces its own limit in a deployed host, but the acceptance suite drives the API
/// through a test server that does not, and a bound only production honours is a bound that
/// goes untested. This middleware makes <c>L2-096</c> true on both.
/// </remarks>
public sealed class RequestBodyLimitMiddleware
{
    public const long MaxBytes = 1024 * 1024;

    private readonly RequestDelegate _next;

    public RequestBodyLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.ContentLength > MaxBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "The request body is too large.",
                Status = StatusCodes.Status413PayloadTooLarge,
            });

            return;
        }

        await _next(context);
    }
}
