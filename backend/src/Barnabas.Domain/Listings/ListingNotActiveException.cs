namespace Barnabas.Domain.Listings;

/// <summary>
/// Raised when a transition is applied to a listing that has already had one.
/// </summary>
/// <remarks>Mapped to 409 Conflict.</remarks>
public sealed class ListingNotActiveException : Exception
{
    public ListingNotActiveException(Guid listingId)
        : base("The listing is no longer active.")
        => ListingId = listingId;

    public Guid ListingId { get; }
}
