using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Barnabas.Application.Common.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Barnabas.Api.Filters;

/// <summary>
/// Rejects a field a command forbids for its kind, while the value is still there to see.
/// </summary>
/// <remarks>
/// Unknown and forbidden are not the same thing. A field the command does not recognise is
/// ignored, so a client sending a superseded field is not broken by a deployment. A field the
/// command explicitly forbids is rejected with 400 naming it.
/// <para>
/// The distinction is load-bearing rather than pedantic. <c>L2-027</c> requires a Lend listing
/// submitted with a price to be rejected, and <c>PostLendListingCommand</c> has no
/// <c>Price</c> property to bind one to - so if forbidden fields were merely unknown, the price
/// would be silently dropped and the response would be 201. The requirement would be
/// unimplementable by construction.
/// </para>
/// <para>
/// This runs as a resource filter, which is the last point before model binding. Inspecting
/// afterwards would be too late, because by then the forbidden value has already been discarded.
/// </para>
/// </remarks>
public sealed class ForbiddenFieldInspector : IAsyncResourceFilter
{
    private static readonly ConcurrentDictionary<string, IReadOnlySet<string>> Declared = new(StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var forbidden = ForbiddenFieldsFor(context.ActionDescriptor);

        if (forbidden.Count == 0)
        {
            await next();

            return;
        }

        var offending = await FindOffendingFieldsAsync(context.HttpContext.Request, forbidden);

        if (offending.Count > 0)
        {
            var errors = offending.ToDictionary(
                field => field,
                field => new[] { $"The field '{field}' is not accepted for this kind of listing." },
                StringComparer.Ordinal);

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Title = "One or more fields are not accepted.",
                Status = StatusCodes.Status400BadRequest,
            });

            return;
        }

        await next();
    }

    private static async Task<IReadOnlyList<string>> FindOffendingFieldsAsync(
        HttpRequest request,
        IReadOnlySet<string> forbidden)
    {
        if (request.ContentLength is null or 0 || !IsJson(request.ContentType))
        {
            return [];
        }

        request.EnableBuffering();

        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: request.HttpContext.RequestAborted);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return [.. document.RootElement
                .EnumerateObject()
                .Where(property => forbidden.Contains(property.Name))
                .Select(property => property.Name)];
        }
        catch (JsonException)
        {
            // Malformed JSON is the model binder's complaint to make, not this filter's.
            return [];
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static bool IsJson(string? contentType) =>
        contentType is not null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads what the action's command declares, rather than holding a list of its own.
    /// </summary>
    private static IReadOnlySet<string> ForbiddenFieldsFor(ActionDescriptor descriptor)
    {
        if (descriptor is not ControllerActionDescriptor action)
        {
            return None;
        }

        return Declared.GetOrAdd(action.Id, _ => Discover(action));
    }

    private static IReadOnlySet<string> Discover(ControllerActionDescriptor action)
    {
        var declared = action.MethodInfo
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.IsAssignableTo(typeof(IForbidFields)))
            .Select(type => type.GetProperty(
                nameof(IForbidFields.ForbiddenFields),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(property => property is not null)
            .Select(property => property!.GetValue(null))
            .OfType<IReadOnlySet<string>>()
            .SelectMany(fields => fields);

        return new HashSet<string>(declared, StringComparer.OrdinalIgnoreCase);
    }
}
