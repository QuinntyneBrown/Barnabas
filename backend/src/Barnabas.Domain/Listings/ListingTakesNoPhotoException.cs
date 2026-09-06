namespace Barnabas.Domain.Listings;

/// <summary>
/// Raised when a photo is attached to a Help listing.
/// </summary>
/// <remarks>
/// Help offers time rather than an object, so there is nothing to photograph. This is the same
/// rule <c>L2-030</c> states from the posting side, held here so no path can get round it.
/// </remarks>
public sealed class ListingTakesNoPhotoException : Exception
{
    public ListingTakesNoPhotoException(Guid listingId)
        : base("That kind of listing does not carry a photo.") => ListingId = listingId;

    public Guid ListingId { get; }
}
