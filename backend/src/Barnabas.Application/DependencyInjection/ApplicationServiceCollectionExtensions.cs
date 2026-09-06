using System.Reflection;
using Barnabas.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Barnabas.Application.DependencyInjection;

/// <summary>
/// Registers the application layer: the mediator, its pipeline, and every validator.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBarnabasApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));

        // Order is load-bearing and is asserted by the pipeline's own documentation: validation
        // runs first, so a request that is both malformed and unauthorised is reported as
        // malformed rather than disclosing anything about the resource.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorisationBehaviour<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
