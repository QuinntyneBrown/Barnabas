using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Middleware;

/// <summary>
/// Refuses a body larger than the endpoint accepts, before anything tries to read it.
/// </summary>
/// <remarks>
/// Kestrel enforces its own limit in a deployed host, but the acceptance suite drives the API
/// through a test server that does not, and a bound only production honours is a bound that
/// goes untested. This middleware makes <c>L2-096</c> true on both.
/// <para>
/// The limit is per endpoint. One MB refuses the oversized JSON body <c>L2-096 AC2</c> asks
/// about; the upload route declares more with <see cref="MaxRequestBodyAttribute"/>, so
/// <c>L2-032</c>'s 2 MB photo is accepted and its 12 MB one is still refused. Reading the
/// endpoint requires routing to have run, which is why <c>UseRouting</c> is called explicitly
/// ahead of this rather than left to be inserted at the front of the pipeline.
/// </para>
/// </remarks>
public sealed class RequestBodyLimitMiddleware
{
    public const long MaxBytes = 1024 * 1024;

    private readonly RequestDelegate _next;

    public RequestBodyLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var allowed = context.GetEndpoint()?.Metadata.GetMetadata<MaxRequestBodyAttribute>()?.Bytes
            ?? MaxBytes;

        if (context.Request.ContentLength > allowed)
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
