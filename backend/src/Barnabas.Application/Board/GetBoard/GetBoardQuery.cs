using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// Asks for the active listings of the caller's congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The global query filter supplies that predicate, so there is
/// nothing here for a caller to tamper with and nothing for a handler to forget.
/// </remarks>
public sealed record GetBoardQuery(ListingKind? Kind, int Limit, string? Cursor) : IRequest<BoardPage>;
