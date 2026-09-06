using Barnabas.Domain.Listings;
using FluentValidation;

namespace Barnabas.Application.Listings.PostGiveListing;

/// <summary>The bounds a Give listing accepts.</summary>
public sealed class PostGiveListingCommandValidator : AbstractValidator<PostGiveListingCommand>
{
    public PostGiveListingCommandValidator()
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
