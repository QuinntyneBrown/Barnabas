using Barnabas.Domain.Listings;
using FluentValidation;

namespace Barnabas.Application.Listings.PostLendListing;

/// <summary>
/// The bounds a Lend listing accepts.
/// </summary>
/// <remarks>
/// The lengths are stated on the entity and enforced here. That is not a duplication: the entity
/// keeps its own invariants, and this states what the system will accept from outside and can
/// report back to a member by field name.
/// </remarks>
public sealed class PostLendListingCommandValidator : AbstractValidator<PostLendListingCommand>
{
    public PostLendListingCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(Listing.TitleMaxLength)
            .WithMessage($"Give the listing a title of at most {Listing.TitleMaxLength} characters.");

        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(Listing.DescriptionMaxLength)
            .WithMessage($"Keep the description to at most {Listing.DescriptionMaxLength} characters.");

        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);

        RuleFor(command => command.Neighbourhood).NotEmpty().MaximumLength(200);

        RuleFor(command => command.ReturnBy)
            .NotNull()
            .Must(returnBy => returnBy != default(DateOnly))
            .WithMessage("Say when you expect the item back.");
    }
}
