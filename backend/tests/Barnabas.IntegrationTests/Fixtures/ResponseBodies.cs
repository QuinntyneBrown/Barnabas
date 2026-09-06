namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// The response shapes the acceptance tests read.
/// </summary>
/// <remarks>
/// Declared here rather than reused from the application layer on purpose. A test that
/// deserialised into the very type the handler returned would still pass if the wire contract
/// changed underneath it; these are a second, independent statement of what the API promises.
/// Enums are read as strings because that is what the web client's own models expect.
/// </remarks>
public sealed record PostedListing(Guid ListingId);

public sealed record BoardListingBody(
    Guid ListingId,
    string Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price,
    string? OfferInOwnWords);

public sealed record BoardPageBody(
    IReadOnlyList<BoardListingBody> Listings,
    string? NextCursor,
    IReadOnlyDictionary<string, int> Counts);

public sealed record ListingDetailBody(
    Guid ListingId,
    string Kind,
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    string Status,
    Guid OwnerId,
    string OwnerDisplayName,
    decimal? Price,
    DateOnly? ReturnBy,
    DateTimeOffset PostedAt,
    bool IsOwnedByCaller);

public sealed record MyListingBody(
    Guid ListingId,
    string Kind,
    string Status,
    string Title,
    decimal? Price,
    int OpenRequestCount);

public sealed record ClosedOutListing(Guid ListingId, string Status);
