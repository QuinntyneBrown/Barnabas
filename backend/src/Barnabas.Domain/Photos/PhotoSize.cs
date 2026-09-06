namespace Barnabas.Domain.Photos;

/// <summary>
/// The two sizes a listing photo is kept at.
/// </summary>
/// <remarks>
/// Two rather than a spectrum. The board shows a mosaic of small fields and the listing screen
/// shows one large one, and those are the only two places a photo appears — a resize service
/// taking arbitrary dimensions would be a piece of infrastructure nothing asked for.
/// </remarks>
public enum PhotoSize
{
    /// <summary>What the listing screen shows. Bounded, not the bytes that were uploaded.</summary>
    Full = 0,

    /// <summary>What a placard on the board shows. L2-106 AC1.</summary>
    Board = 1,
}
