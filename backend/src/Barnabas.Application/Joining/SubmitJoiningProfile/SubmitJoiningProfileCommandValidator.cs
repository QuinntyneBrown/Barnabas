using Barnabas.Domain.Members;
using FluentValidation;

namespace Barnabas.Application.Joining.SubmitJoiningProfile;

/// <summary>
/// The bounds a profile accepts.
/// </summary>
/// <remarks>
/// The neighbourhood's membership of the congregation's own set is not checked here. A validator
/// holds no congregation, and the rule is about the relationship between two things rather than
/// the shape of one field, so the handler settles it — and answers 400 naming the field, which is
/// what <c>L2-002 AC2</c> asks for.
/// </remarks>
public sealed class SubmitJoiningProfileCommandValidator : AbstractValidator<SubmitJoiningProfileCommand>
{
    public SubmitJoiningProfileCommandValidator()
    {
        RuleFor(command => command.JoiningToken).NotEmpty();

        RuleFor(command => command.EmailAddress)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320)
            .WithMessage("Give an email address we can send your sign-in link to.");

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(Member.DisplayNameMaxLength)
            .WithMessage("Say what the congregation should call you.");

        RuleFor(command => command.Neighbourhood)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Choose your neighbourhood.");

        RuleFor(command => command.ReasonForJoining)
            .MaximumLength(Member.ReasonForJoiningMaxLength)
            .When(command => command.ReasonForJoining is not null);
    }
}
