using Barnabas.Domain.Listings;
using FluentValidation;

namespace Barnabas.Application.Listings.EditListing;

/// <summary>
/// The bounds an edit accepts.
/// </summary>
/// <remarks>
/// The same bounds the creation forms apply. An edit that could store what a post could not
/// would be a way around them.
/// </remarks>
public sealed class EditListingCommandValidator : AbstractValidator<EditListingCommand>
{
    public EditListingCommandValidator()
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
    }
}
