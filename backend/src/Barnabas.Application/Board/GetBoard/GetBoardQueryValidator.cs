using FluentValidation;

namespace Barnabas.Application.Board.GetBoard;

public sealed class GetBoardQueryValidator : AbstractValidator<GetBoardQuery>
{
    public const int MaximumLimit = 100;

    public GetBoardQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, MaximumLimit);
        RuleFor(query => query.Kind).IsInEnum().When(query => query.Kind is not null);
    }
}
