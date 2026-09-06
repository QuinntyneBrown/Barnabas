using FluentValidation;
using MediatR;

namespace Barnabas.Application.Common.Behaviours;

/// <summary>
/// Runs every validator registered for a request before its handler.
/// </summary>
/// <remarks>
/// Validation runs as a pipeline stage rather than at the top of each handler. A handler
/// that validates its own input is a handler that can forget to, and the failure mode is
/// silent: malformed data reaches the domain and either throws somewhere less helpful or
/// persists. Running validation ahead of the handler means a handler that is reached can
/// be written as though its input is sound.
/// <para>
/// This runs before <see cref="AuthorisationBehaviour{TRequest,TResponse}"/>, so a request
/// that is both malformed and unauthorised is reported as malformed. That ordering
/// discloses nothing, because a caller learns only about their own input.
/// </para>
/// </remarks>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToArray();

        if (failures.Length > 0)
        {
            // The message names the field at fault and never echoes the submitted value.
            // Echoing is how a validation message becomes a reflection vector, and the
            // member already knows what they typed.
            throw new ValidationException(failures);
        }

        return await next();
    }
}
