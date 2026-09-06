using FluentValidation;

namespace Barnabas.Application.Access.RefreshSession;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
