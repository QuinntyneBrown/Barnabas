using FluentValidation;

namespace Barnabas.Application.Notifications.Preferences;

public sealed class SetNotificationPreferencesCommandValidator
    : AbstractValidator<SetNotificationPreferencesCommand>
{
    public SetNotificationPreferencesCommandValidator()
    {
        RuleFor(command => command.Preferences)
            .NotNull()
            .Must(preferences => preferences is null || preferences.All(p => Enum.IsDefined(p.Kind)))
            .WithMessage("That is not a kind of notification.")
            .Must(preferences => preferences is null
                || preferences.Select(p => p.Kind).Distinct().Count() == preferences.Count)
            .WithMessage("Say once whether you want each kind.");
    }
}
