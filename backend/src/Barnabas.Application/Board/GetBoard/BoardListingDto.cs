using Barnabas.Domain.Listings;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// One placard on the board.
/// </summary>
/// <remarks>
/// A read model rather than the entity. The board needs the owner's display name, which lives on
/// the member aggregate, and projecting once here is what stops the screen assembling itself
/// from several calls.
/// <para>
/// <see cref="Kind"/> is carried as data, not as a colour. The screen renders it as text as well
/// as a field colour, which is what L2-044 requires: colour alone cannot carry the meaning for a
/// member with a colour vision deficiency, or for anyone reading in sunlight.
/// </para>
/// </remarks>
public sealed record BoardListingDto(
    Guid ListingId,
    ListingKind Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price,
    string? OfferInOwnWords);
