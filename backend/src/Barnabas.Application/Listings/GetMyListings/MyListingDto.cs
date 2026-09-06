using Barnabas.Domain.Listings;

namespace Barnabas.Application.Listings.GetMyListings;

/// <summary>
/// One of the caller's own listings, with how many people are waiting on it.
/// </summary>
/// <remarks>
/// The open request count is the reason this screen exists rather than being a filtered board:
/// what an owner needs to know is which of their listings somebody is waiting on.
/// <para>
/// <see cref="CanBeRestored"/> is answered here rather than worked out by the screen. A closed-out
/// Lend listing and a shelved one are both <see cref="ListingStatus.Archived"/>, so the status
/// cannot tell them apart - and the rule that can belongs on the entity, not in a template.
/// </para>
/// </remarks>
public sealed record MyListingDto(
    Guid ListingId,
    ListingKind Kind,
    ListingStatus Status,
    string Title,
    decimal? Price,
    int OpenRequestCount,
    bool CanBeRestored);
