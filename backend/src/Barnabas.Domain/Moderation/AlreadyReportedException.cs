namespace Barnabas.Domain.Moderation;

/// <summary>
/// Raised when a member reports a listing they have already reported.
/// </summary>
/// <remarks>
/// Once per member, permanently. Unlike a request — which may be made again after a decline —
/// there is no second reading of the same complaint from the same person, so the index that
/// enforces this carries no filter.
/// </remarks>
public sealed class AlreadyReportedException : Exception
{
    public AlreadyReportedException(Guid listingId)
        : base("You have already reported that listing.") => ListingId = listingId;

    public Guid ListingId { get; }
}
