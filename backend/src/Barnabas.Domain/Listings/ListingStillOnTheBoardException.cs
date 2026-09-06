namespace Barnabas.Domain.Listings;

/// <summary>
/// Raised when a listing still on the board is asked to be deleted.
/// </summary>
/// <remarks>
/// Archiving first is not ceremony. It is the undoable step between a member deciding to stop
/// offering something and destroying the record of it, along with any conversation it opened.
/// </remarks>
public sealed class ListingStillOnTheBoardException : Exception
{
    public ListingStillOnTheBoardException(Guid listingId)
        : base("Take the listing off the board before deleting it.") => ListingId = listingId;

    public Guid ListingId { get; }
}
