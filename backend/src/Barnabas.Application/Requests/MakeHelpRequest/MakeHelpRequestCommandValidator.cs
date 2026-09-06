using Barnabas.Domain.Requests;
using FluentValidation;

namespace Barnabas.Application.Requests.MakeHelpRequest;

/// <summary>
/// The bounds a Help request accepts.
/// </summary>
/// <remarks>
/// That the window is <em>one the listing declared</em> is not checked here. A validator holds
/// no listing, and the rule is about the relationship between two things rather than about the
/// shape of one field, so the handler settles it.
/// </remarks>
public sealed class MakeHelpRequestCommandValidator : AbstractValidator<MakeHelpRequestCommand>
{
    public MakeHelpRequestCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(ListingRequest.MessageMaxLength)
            .WithMessage("Say what you need a hand with. A sentence or two is plenty.");

        RuleFor(command => command.AvailabilityWindowId)
            .NotNull()
            .NotEqual(Guid.Empty)
            .WithMessage("Choose one of the windows on offer.");
    }
}
