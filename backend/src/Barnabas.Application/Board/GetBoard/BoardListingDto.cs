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
/// <para>
/// <see cref="PhotoUrl"/> is the board's own rendition rather than the uploaded bytes, so a
/// mosaic of thirty placards fetches thirty small images. L2-106 AC1.
/// </para>
/// <para>
/// <see cref="Availability"/> is present on Help placards and null on the others. An offer of
/// time that does not say when is not much of an offer, so the days are on the board rather than
/// one screen further in.
/// </para>
/// </remarks>
public sealed record BoardListingDto(
    Guid ListingId,
    ListingKind Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price,
    string? OfferInOwnWords,
    string? Availability,
    string? PhotoUrl);
