namespace Barnabas.Domain.Listings;

/// <summary>Raised when a moderator decides about a listing nobody has reported.</summary>
public sealed class ListingNotFlaggedException : Exception
{
    public ListingNotFlaggedException(Guid listingId)
        : base("That listing has not been reported.") => ListingId = listingId;

    public Guid ListingId { get; }
}
