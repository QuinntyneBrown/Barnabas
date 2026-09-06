using FluentValidation;

namespace Barnabas.Application.Search.SearchListings;

/// <summary>
/// What a search will accept.
/// </summary>
/// <remarks>
/// The limit is absent for the same reason it is absent from the board's validator: a page size
/// is a transport hint rather than member content, so it is clamped at the edge.
/// </remarks>
public sealed class SearchListingsQueryValidator : AbstractValidator<SearchListingsQuery>
{
    public const int TermMaxLength = 100;

    public SearchListingsQueryValidator()
    {
        RuleFor(query => query.Term)
            .NotEmpty()
            .MaximumLength(TermMaxLength)
            .WithMessage($"Search for something, in at most {TermMaxLength} characters.");

        RuleFor(query => query.Kind).IsInEnum().When(query => query.Kind is not null);

        RuleFor(query => query.Neighbourhood).MaximumLength(200);

        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(0m)
            .When(query => query.MaxPrice is not null);
    }
}
