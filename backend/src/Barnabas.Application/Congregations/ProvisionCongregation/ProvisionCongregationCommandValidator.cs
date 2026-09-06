using Barnabas.Domain.Congregations;
using FluentValidation;

namespace Barnabas.Application.Congregations.ProvisionCongregation;

/// <summary>
/// The bounds provisioning accepts. The values are settled in ADR-0002.
/// </summary>
public sealed class ProvisionCongregationCommandValidator : AbstractValidator<ProvisionCongregationCommand>
{
    public ProvisionCongregationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Congregation.NameMaxLength)
            .WithMessage($"Name the congregation, in at most {Congregation.NameMaxLength} characters.");

        RuleFor(command => command.Slug)
            .NotEmpty()
            .Must(slug => Congregation.IsValidSlug(Congregation.NormaliseSlug(slug ?? string.Empty)))
            .WithMessage(
                $"A slug is {Congregation.SlugMinLength} to {Congregation.SlugMaxLength} lower-case "
                + "letters, digits and hyphens, like st-aidans.");

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

        RuleFor(command => command.FoundingModeratorEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320)
            .WithMessage("Give the founding moderator's email address.");

        RuleFor(command => command.FoundingModeratorDisplayName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
