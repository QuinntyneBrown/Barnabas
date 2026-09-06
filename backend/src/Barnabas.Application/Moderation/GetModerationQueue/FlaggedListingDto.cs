using Barnabas.Domain.Listings;

namespace Barnabas.Application.Moderation.GetModerationQueue;

/// <summary>A flagged listing, its poster, and every open complaint about it.</summary>
public sealed record FlaggedListingDto(
    Guid ListingId,
    ListingKind Kind,
    string Title,
    string Description,
    Guid OwnerId,
    string OwnerDisplayName,
    DateTimeOffset FlaggedAt,
    IReadOnlyList<ReportedListingDto> Reports);
