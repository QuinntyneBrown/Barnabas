using Barnabas.Domain.Requests;
using FluentValidation;

namespace Barnabas.Application.Requests.MakePurchaseRequest;

public sealed class MakePurchaseRequestCommandValidator : AbstractValidator<MakePurchaseRequestCommand>
{
    public MakePurchaseRequestCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(ListingRequest.MessageMaxLength)
            .WithMessage("Say a word about what you are after. A sentence or two is plenty.");

        RuleFor(command => command.PickupAt)
            .NotNull()
            .Must(pickupAt => pickupAt != default(DateTimeOffset))
            .WithMessage("Say when you could collect it.");
    }
}
