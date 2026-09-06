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

public sealed record ProvisionedCongregation(Guid CongregationId, string Name, string Slug);

public sealed record ConfiguredCongregation(
    Guid CongregationId,
    string Name,
    string Slug,
    IReadOnlyList<string> Neighbourhoods);

public sealed record CongregationBody(
    Guid CongregationId,
    string Name,
    string Slug,
    IReadOnlyList<string> Neighbourhoods);

public sealed record DesignatedModerator(Guid MemberId, string Role);

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
    bool IsOwnedByCaller,
    string? Condition = null,
    IReadOnlyList<AvailabilityWindowBody>? AvailabilityWindows = null);

/// <summary>One window a Help listing declares, and the identifier a request names it by.</summary>
public sealed record AvailabilityWindowBody(
    Guid AvailabilityWindowId,
    string Day,
    TimeOnly StartsAt,
    TimeOnly EndsAt);

public sealed record MyListingBody(
    Guid ListingId,
    string Kind,
    string Status,
    string Title,
    decimal? Price,
    int OpenRequestCount);

public sealed record ClosedOutListing(Guid ListingId, string Status);

public sealed record MadeRequest(Guid RequestId);

public sealed record IncomingRequestBody(
    Guid RequestId,
    Guid RequesterId,
    string RequesterDisplayName,
    Guid ListingId,
    string ListingTitle,
    string Kind,
    string Message,
    string Terms,
    string Status,
    DateTimeOffset MadeAt);

public sealed record MyRequestBody(
    Guid RequestId,
    Guid ListingId,
    string ListingTitle,
    string Kind,
    string OwnerDisplayName,
    string Neighbourhood,
    string Terms,
    string Status,
    DateTimeOffset MadeAt,
    Guid? ThreadId);

public sealed record AcceptedRequest(Guid RequestId, Guid ThreadId);

public sealed record DeclinedRequest(Guid RequestId, string Status);

public sealed record ThreadSummaryBody(
    Guid ThreadId,
    Guid OtherMemberId,
    string OtherMemberDisplayName,
    Guid ListingId,
    string ListingTitle,
    string LatestMessage,
    DateTimeOffset? LatestAt,
    bool Unread);

public sealed record MessageBody(
    Guid MessageId,
    Guid SenderId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset SentAt,
    bool SentByCaller);

public sealed record ThreadDetailBody(
    Guid ThreadId,
    Guid ListingId,
    string ListingTitle,
    Guid OtherMemberId,
    string OtherMemberDisplayName,
    string RequestStatus,
    IReadOnlyList<MessageBody> Messages);
