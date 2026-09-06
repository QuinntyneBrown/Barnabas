namespace Barnabas.Application.Members.ExportMyData;

/// <summary>One listing this member posted.</summary>
public sealed record ExportedListing(
    Guid ListingId,
    string Kind,
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    string Status,
    decimal? Price,
    DateTimeOffset PostedAt,
    string? PhotoUrl);
