using FluentValidation;

namespace Barnabas.Application.Access.RequestSignInLink;

public sealed class RequestSignInLinkCommandValidator : AbstractValidator<RequestSignInLinkCommand>
{
    public RequestSignInLinkCommandValidator()
    {
        RuleFor(command => command.EmailAddress)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress()
            .WithMessage("Enter the email address the parish office has for you.");
    }
}
