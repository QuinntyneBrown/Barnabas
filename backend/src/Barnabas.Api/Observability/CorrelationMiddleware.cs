using Microsoft.Extensions.Primitives;

namespace Barnabas.Api.Observability;

/// <summary>
/// Gives every request an identifier, and puts it on everything logged while it runs.
/// </summary>
/// <remarks>
/// A log scope rather than a value each call site remembers to pass. Every entry a request
/// produces is inside this scope, including ones from the framework and from EF Core, which is
/// what <c>L2-117 AC1</c> means by "every log entry arising from it".
/// <para>
/// The caller's own identifier is reused when they supply one. That is the whole point of the
/// header: a request that crossed a proxy, a queue and this API should be one line of enquiry
/// rather than three, and minting a fresh identifier here would break the chain at exactly the
/// boundary it was meant to cross.
/// </para>
/// <para>
/// It is echoed on the response, so a member reporting a fault can be asked for a number that
/// finds it.
/// </para>
/// </remarks>
public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The longest identifier accepted from a caller.</summary>
    /// <remarks>
    /// A caller-supplied value ends up in every log line the request writes, so an unbounded one
    /// is a way to write as much of somebody else's log as you like.
    /// </remarks>
    private const int MaxLength = 128;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Supplied(context.Request.Headers) ?? Guid.NewGuid().ToString("n");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // The endpoint is part of the scope rather than left to each message, because L2-117 AC3
        // asks a handled failure to carry it and the exception handler does not know the route.
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId,
                   ["Endpoint"] = $"{context.Request.Method} {context.Request.Path}",
               }))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// The caller's identifier, if it is one we are willing to write down.
    /// </summary>
    /// <remarks>
    /// Restricted to characters that cannot break a log line or a terminal. A newline in a
    /// correlation identifier is how one forged entry becomes two convincing ones.
    /// </remarks>
    private static string? Supplied(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderName, out StringValues values))
        {
            return null;
        }

        var supplied = values.ToString().Trim();

        if (supplied.Length == 0 || supplied.Length > MaxLength)
        {
            return null;
        }

        foreach (var character in supplied)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.' or ':'))
            {
                return null;
            }
        }

        return supplied;
    }
}
