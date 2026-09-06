using Barnabas.Domain.Congregations;
using FluentValidation;

namespace Barnabas.Application.Congregations.ConfigureCongregation;

public sealed class ConfigureCongregationCommandValidator : AbstractValidator<ConfigureCongregationCommand>
{
    public ConfigureCongregationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Congregation.NameMaxLength);

        RuleFor(command => command.Neighbourhoods)
            .NotNull()
            .Must(names => names is { Count: > 0 })
            .WithMessage("Name at least one neighbourhood.")
            .Must(names => names is null || names.Count <= Congregation.MaxNeighbourhoods)
            .WithMessage($"Name at most {Congregation.MaxNeighbourhoods} neighbourhoods.")
            .Must(names => names is null || names.All(name => !string.IsNullOrWhiteSpace(name)))
            .WithMessage("Every neighbourhood needs a name.")
            .Must(names => names is null
                || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count)
            .WithMessage("Name each neighbourhood once.");
    }
}
