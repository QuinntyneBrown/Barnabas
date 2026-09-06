namespace Barnabas.Domain.Listings;

/// <summary>
/// Raised when a listing that cannot go back on the board is asked to.
/// </summary>
/// <remarks>
/// Distinct from a listing simply not being archived. A listing that was closed out is archived
/// and still may not be restored, because it has already served its purpose - so the two refusals
/// have different reasons and say so.
/// </remarks>
public sealed class ListingNotRestorableException : Exception
{
    public ListingNotRestorableException(Guid listingId)
        : base("The listing cannot be put back on the board.") => ListingId = listingId;

    public Guid ListingId { get; }
}
