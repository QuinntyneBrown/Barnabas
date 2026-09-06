using Barnabas.Domain.Listings;

namespace Barnabas.Domain.Requests;

/// <summary>
/// The four conditions a request has to satisfy before it is created.
/// </summary>
/// <remarks>
/// This policy exists so the common case produces a clear answer without touching the
/// database twice. It is <em>not</em> what makes the duplicate rule true: two simultaneous
/// requests both pass the check before either commits, so a partial unique index on
/// <c>(ListingId, RequesterId) where Status = Pending</c> decides that case. Neither is
/// redundant. Deleting the index because "the policy covers it" would reopen L2-062;
/// deleting the policy because "the index covers it" would degrade every duplicate into a
/// raw constraint violation.
/// </remarks>
public static class RequestEligibilityPolicy
{
    public static RequestEligibility Check(Listing listing, Guid requesterId, ListingKind endpointKind)
    {
        ArgumentNullException.ThrowIfNull(listing);

        // A member cannot ask themselves for something they already have.
        if (listing.IsOwnedBy(requesterId))
        {
            return RequestEligibility.OwnListing;
        }

        // The endpoint has to match the listing. Nothing else ties the two together, so
        // without this a Sell listing could acquire a request carrying loan terms.
        if (listing.Kind != endpointKind)
        {
            return RequestEligibility.KindMismatch;
        }

        // A listing that has been sold or archived is still reachable by its identifier.
        if (!listing.IsActive)
        {
            return RequestEligibility.ListingNotActive;
        }

        return RequestEligibility.Eligible;
    }
}
