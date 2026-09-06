using Barnabas.Domain.Requests;
using FluentValidation;

namespace Barnabas.Application.Requests.MakeGiftRequest;

public sealed class MakeGiftRequestCommandValidator : AbstractValidator<MakeGiftRequestCommand>
{
    public MakeGiftRequestCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(ListingRequest.MessageMaxLength)
            .WithMessage("Say a word about why you would like it. A sentence or two is plenty.");

        RuleFor(command => command.PickupAt)
            .NotNull()
            .Must(pickupAt => pickupAt != default(DateTimeOffset))
            .WithMessage("Say when you could collect it.");
    }
}
