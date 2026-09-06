using Barnabas.Domain.Congregations;
using FluentValidation;

namespace Barnabas.Application.Joining.RedeemInviteCode;

public sealed class RedeemInviteCodeCommandValidator : AbstractValidator<RedeemInviteCodeCommand>
{
    public RedeemInviteCodeCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(32)
            .WithMessage($"An invite code is {InviteCode.Length} characters.");
    }
}
