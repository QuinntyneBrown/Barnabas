using System.Reflection;
using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Barnabas.Application.Common.Behaviours;

/// <summary>
/// Enforces whatever authorisation a request declares, before its handler runs.
/// </summary>
/// <remarks>
/// A request declares what it demands by implementing <see cref="IRequireRole"/>,
/// <see cref="IRequireOwnership{TResource}"/>, or neither. This behaviour reads those
/// declarations rather than knowing about individual commands, which is what stops a handler
/// forgetting the check: a handler that is reached has already been authorised, and failure is
/// reported before any state is read or written.
/// <para>
/// It runs after <see cref="ValidationBehaviour{TRequest,TResponse}"/>, so a request that is
/// both malformed and unauthorised is reported as malformed. That ordering discloses nothing,
/// because a caller learns only about their own input.
/// </para>
/// </remarks>
public sealed class AuthorisationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Type? OwnershipDeclaration = typeof(TRequest)
        .GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequireOwnership<>));

    private static readonly Func<IServiceProvider, Guid, CancellationToken, Task<IOwnedResource?>>? LoadOwnedResource =
        BuildLoader();

    private readonly ICongregationContext _context;

    // Service location, deliberately. The resource type is known only per closed request type,
    // so the lookup cannot be a constructor dependency of an open generic behaviour.
    private readonly IServiceProvider _services;

    public AuthorisationBehaviour(ICongregationContext context, IServiceProvider services)
    {
        _context = context;
        _services = services;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // The unresolved case is checked first because reading Role without a session throws,
        // and an anonymous caller on a role-gated request would then get 500 where L2-095 asks
        // for 403. Having no role at all is the clearest case of not having the required one.
        if (request is IRequireRole roleRequirement
            && (!_context.IsResolved || _context.Role < roleRequirement.RequiredRole))
        {
            throw new ForbiddenException();
        }

        if (LoadOwnedResource is not null && OwnershipDeclaration is not null)
        {
            var resourceId = (Guid)OwnershipDeclaration
                .GetProperty(nameof(IRequireOwnership<IOwnedResource>.ResourceId))!
                .GetValue(request)!;

            var resource = await LoadOwnedResource(_services, resourceId, cancellationToken);

            // A resource that is absent is not a refusal. The lookup reads through the
            // congregation filter, so another congregation's row is already gone by the time
            // it is asked for, and answering 403 here would confirm the row exists. The
            // handler's own not-found path produces the 404 that L2-089 requires.
            if (resource is not null && resource.OwnerId != _context.MemberId)
            {
                throw new ForbiddenException();
            }
        }

        return await next();
    }

    private static Func<IServiceProvider, Guid, CancellationToken, Task<IOwnedResource?>>? BuildLoader()
    {
        if (OwnershipDeclaration is null)
        {
            return null;
        }

        var resourceType = OwnershipDeclaration.GetGenericArguments()[0];

        return typeof(AuthorisationBehaviour<TRequest, TResponse>)
            .GetMethod(nameof(LoadAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resourceType)
            .CreateDelegate<Func<IServiceProvider, Guid, CancellationToken, Task<IOwnedResource?>>>();
    }

    private static async Task<IOwnedResource?> LoadAsync<TResource>(
        IServiceProvider services,
        Guid resourceId,
        CancellationToken cancellationToken)
        where TResource : class, IOwnedResource
    {
        var lookup = services.GetRequiredService<IOwnerLookup<TResource>>();

        return await lookup.FindAsync(resourceId, cancellationToken);
    }
}
