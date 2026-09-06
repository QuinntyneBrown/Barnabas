using Barnabas.Domain.Members;
using FluentValidation;

namespace Barnabas.Application.Members.EditMyProfile;

public sealed class EditMyProfileCommandValidator : AbstractValidator<EditMyProfileCommand>
{
    public EditMyProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(Member.DisplayNameMaxLength)
            .WithMessage("Say what the congregation should call you.");

        RuleFor(command => command.Neighbourhood)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Choose your neighbourhood.");

        RuleFor(command => command.Description)
            .MaximumLength(Member.DescriptionMaxLength)
            .WithMessage($"Keep it to at most {Member.DescriptionMaxLength} characters.")
            .When(command => command.Description is not null);

        RuleFor(command => command.HelpTags)
            .Must(tags => tags is null || tags.Count <= Member.MaxHelpTags)
            .WithMessage($"Declare at most {Member.MaxHelpTags} kinds of help.");
    }
}
