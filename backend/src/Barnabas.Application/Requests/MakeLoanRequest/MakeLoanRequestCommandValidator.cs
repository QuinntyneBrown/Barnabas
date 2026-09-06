using Barnabas.Domain.Requests;
using FluentValidation;

namespace Barnabas.Application.Requests.MakeLoanRequest;

public sealed class MakeLoanRequestCommandValidator : AbstractValidator<MakeLoanRequestCommand>
{
    public MakeLoanRequestCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(ListingRequest.MessageMaxLength)
            .WithMessage("Say what you need it for. A sentence or two is plenty.");

        RuleFor(command => command.PickupOn)
            .NotEqual(default(DateOnly))
            .WithMessage("Say when you could pick it up.");

        RuleFor(command => command.ReturnBy)
            .NotEqual(default(DateOnly))
            .WithMessage("Say when you would bring it back.");

        RuleFor(command => command.ReturnBy)
            .GreaterThanOrEqualTo(command => command.PickupOn)
            .When(command => command.PickupOn != default && command.ReturnBy != default)
            .WithMessage("The return date cannot be before the pickup date.");

        // Not decorative. A loan the requester has not acknowledged as a loan is precisely the
        // misunderstanding the field exists to prevent, so an unticked box is a refusal.
        RuleFor(command => command.LoanAcknowledged)
            .Equal(true)
            .WithMessage("Acknowledge that the item stays the owner's.");
    }
}
