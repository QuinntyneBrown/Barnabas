using Barnabas.Domain.Listings;
using FluentValidation;

namespace Barnabas.Application.Listings.PostSellListing;

/// <summary>
/// The bounds a Sell listing accepts.
/// </summary>
/// <remarks>
/// The ceiling exists so that a mistyped price is caught here rather than becoming a listing
/// nobody can take seriously. A parish board sells a bicycle, not a house.
/// </remarks>
public sealed class PostSellListingCommandValidator : AbstractValidator<PostSellListingCommand>
{
    public const decimal MaximumPrice = 100_000m;

    public PostSellListingCommandValidator()
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

        RuleFor(command => command.Condition)
            .NotEmpty()
            .MaximumLength(Listing.ConditionMaxLength)
            .WithMessage("Say what condition it is in.");

        // Two rules rather than one chain: a trailing When applies to every rule before it, so
        // a single chain would have switched off its own NotNull exactly when the price was
        // missing - which is the case it exists to catch.
        RuleFor(command => command.Price)
            .NotNull()
            .WithMessage("Say what you are asking for it.");

        RuleFor(command => command.Price!.Value)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("A price cannot be negative.")
            .LessThanOrEqualTo(MaximumPrice)
            .WithMessage($"Ask at most {MaximumPrice:N0}.")
            .When(command => command.Price is not null)
            .OverridePropertyName(nameof(PostSellListingCommand.Price));
    }
}
