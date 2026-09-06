using Barnabas.Domain.Listings;
using FluentValidation;

namespace Barnabas.Application.Listings.PostHelpListing;

/// <summary>
/// The bounds a Help listing accepts.
/// </summary>
/// <remarks>
/// The window rules are stated against <c>Windows</c> rather than against each element, because
/// the member is looking at one list of windows on one form and a message naming
/// <c>Windows[1].EndsAt</c> would tell them less than a sentence about the window that is wrong.
/// </remarks>
public sealed class PostHelpListingCommandValidator : AbstractValidator<PostHelpListingCommand>
{
    public const int MaximumWindows = 14;

    public PostHelpListingCommandValidator()
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

        RuleFor(command => command.Windows)
            .NotNull()
            .Must(windows => windows is { Count: > 0 })
            .WithMessage("Say when you are free. An offer of time needs at least one window.")
            .Must(windows => windows is null || windows.Count <= MaximumWindows)
            .WithMessage($"Offer at most {MaximumWindows} windows.")
            .Must(AllComplete)
            .WithMessage("Every window needs a day, a start, and an end.")
            .Must(AllEndAfterTheyStart)
            .WithMessage("A window has to end after it starts.")
            .Must(AllDaysAreRealDays)
            .WithMessage("A window's day has to be a day of the week.");
    }

    private static bool AllComplete(IReadOnlyList<AvailabilityWindowInput>? windows) =>
        windows is null || windows.All(w => w.Day is not null && w.StartsAt is not null && w.EndsAt is not null);

    private static bool AllEndAfterTheyStart(IReadOnlyList<AvailabilityWindowInput>? windows) =>
        windows is null || windows.All(w => w.StartsAt is null || w.EndsAt is null || w.EndsAt > w.StartsAt);

    private static bool AllDaysAreRealDays(IReadOnlyList<AvailabilityWindowInput>? windows) =>
        windows is null || windows.All(w => w.Day is null || Enum.IsDefined(w.Day.Value));
}
