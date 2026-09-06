using FluentValidation;

namespace Barnabas.Application.Access.ExchangeSignInToken;

public sealed class ExchangeSignInTokenCommandValidator : AbstractValidator<ExchangeSignInTokenCommand>
{
    public ExchangeSignInTokenCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(256);
    }
}
