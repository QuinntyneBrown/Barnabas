using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.Common;

/// <summary>
/// Loading the listing a request is about, and refusing the ones that cannot be asked for.
/// </summary>
/// <remarks>
/// Shared by the four kinds because the conditions are the same for all of them: not your own,
/// the right kind for the endpoint, still active, and not already asked for. What differs
/// between the kinds is the terms they collect, and that difference stays in the four commands.
/// <para>
/// This is not the same thing as one handler branching on kind. The kind is a parameter here and
/// nothing reads it except <see cref="RequestEligibilityPolicy"/>; no per-kind field is touched.
/// </para>
/// </remarks>
internal static class RequestableListing
{
    /// <summary>
    /// The listing, if this member may ask for it on this endpoint.
    /// </summary>
    public static async Task<Listing> LoadAsync(
        IBarnabasDbContext context,
        Guid listingId,
        Guid requesterId,
        ListingKind endpointKind,
        CancellationToken cancellationToken)
    {
        // Read through the filtered set, so a listing in another congregation is already absent
        // and the answer is not found rather than anything that confirms it exists.
        var listing = await context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == listingId, cancellationToken)
            ?? throw new NotFoundException();

        var eligibility = RequestEligibilityPolicy.Check(listing, requesterId, endpointKind);

        if (eligibility != RequestEligibility.Eligible)
        {
            throw new RequestNotEligibleException(eligibility);
        }

        var alreadyOpen = await context.ListingRequests.AnyAsync(
            existing => existing.ListingId == listing.Id
                && existing.RequesterId == requesterId
                && existing.Status == RequestStatus.Pending,
            cancellationToken);

        if (alreadyOpen)
        {
            throw new RequestNotEligibleException(RequestEligibility.DuplicateRequest);
        }

        return listing;
    }

    /// <summary>
    /// Commits, turning the index's refusal into the same answer the duplicate check gives.
    /// </summary>
    /// <remarks>
    /// The check in <see cref="LoadAsync"/> is not what makes the duplicate rule true: two
    /// simultaneous requests both pass it before either commits. The filtered unique index
    /// decides that case, and this is where its refusal is read.
    /// </remarks>
    public static async Task SaveOrDuplicateAsync(IBarnabasDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new RequestNotEligibleException(RequestEligibility.DuplicateRequest);
        }
    }
}
