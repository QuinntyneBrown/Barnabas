using FluentValidation;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// What the board query will accept.
/// </summary>
/// <remarks>
/// The limit is not validated here, and that is deliberate. L2-105 requires a page size above the
/// maximum to have the maximum <em>applied</em>, where L2-096 requires an over-long field to be
/// <em>rejected</em> naming it. The two are not in conflict once the distinction is drawn: a page
/// size is a transport hint, not something a member typed, so it is clamped at the edge in
/// <c>BoardController</c> and never reaches a rule. A field of member content still gets the 400.
/// </remarks>
public sealed class GetBoardQueryValidator : AbstractValidator<GetBoardQuery>
{
    public GetBoardQueryValidator()
    {
        RuleFor(query => query.Kind).IsInEnum().When(query => query.Kind is not null);
    }
}
