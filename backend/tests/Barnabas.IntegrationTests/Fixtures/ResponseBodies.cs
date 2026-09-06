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

public sealed record IssuedInviteCode(Guid InviteCodeId, string Code, DateTimeOffset ExpiresAt);

public sealed record RedeemedInviteCode(
    string CongregationName,
    IReadOnlyList<string> Neighbourhoods,
    string JoiningToken,
    DateTimeOffset ExpiresAt);

public sealed record JoinedCongregation(Guid MemberId, string CongregationName, string Status);

public sealed record NotificationBody(
    Guid NotificationId,
    string Kind,
    Guid? SubjectMemberId,
    string? SubjectMemberDisplayName,
    Guid? ListingId,
    string? ListingTitle,
    Guid? RequestId,
    Guid? ThreadId,
    DateTimeOffset CreatedAt,
    bool Unread);

public sealed record UnreadCountBody(int Unread);

public sealed record NotificationPreferenceBody(string Kind, bool Enabled);

public sealed record MyProfileBody(
    Guid MemberId,
    string DisplayName,
    string EmailAddress,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    IReadOnlyList<string> Neighbourhoods,
    IReadOnlyList<string> AvailableHelpTags);

public sealed record ProfileListingBody(Guid ListingId, string Kind, string Title, decimal? Price);

public sealed record MemberProfileBody(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    IReadOnlyList<ProfileListingBody> ActiveListings);

public sealed record DirectoryMemberBody(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    IReadOnlyList<string> HelpTags);

public sealed record BoardListingBody(
    Guid ListingId,
    string Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price,
    string? OfferInOwnWords,
    string? PhotoUrl = null);

public sealed record SearchResultBody(
    Guid ListingId,
    string Kind,
    string Title,
    string OwnerDisplayName,
    string Neighbourhood,
    decimal? Price);

public sealed record SearchPageBody(
    string Term,
    IReadOnlyList<SearchResultBody> Results,
    string? NextCursor);

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
    IReadOnlyList<AvailabilityWindowBody>? AvailabilityWindows = null,
    string? PhotoUrl = null);

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

public sealed record ReportedListing(Guid ReportId, Guid ListingId);

public sealed record ListingReportBody(
    Guid ReportId,
    string Reason,
    string? Note,
    Guid ReporterId,
    string ReporterDisplayName,
    DateTimeOffset ReportedAt);

public sealed record FlaggedListingBody(
    Guid ListingId,
    string Kind,
    string Title,
    string Description,
    Guid OwnerId,
    string OwnerDisplayName,
    DateTimeOffset FlaggedAt,
    IReadOnlyList<ListingReportBody> Reports);

public sealed record ReviewedListing(Guid ListingId, string Status, bool Flagged);

public sealed record PendingMemberBody(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    string? ReasonForJoining);

public sealed record DecidedMember(Guid MemberId, string Status);

public sealed record AttachedPhoto(Guid ListingId, Guid PhotoId, string Url, string BoardUrl);

public sealed record ErasedData(
    Guid MemberId,
    int ListingsErased,
    int MessagesErased,
    DateTimeOffset ErasedAt);

public sealed record ExportedProfileBody(
    Guid MemberId,
    string DisplayName,
    string EmailAddress,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    string Role,
    string Status,
    string? ReasonForJoining,
    string CongregationName);

public sealed record ExportedListingBody(
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

public sealed record ExportedRequestBody(
    Guid RequestId,
    Guid ListingId,
    string ListingTitle,
    string Message,
    string Status,
    DateTimeOffset MadeAt);

public sealed record ExportedMessageBody(Guid MessageId, Guid ThreadId, string Body, DateTimeOffset SentAt);

public sealed record MyDataExportBody(
    DateTimeOffset ExportedAt,
    ExportedProfileBody Profile,
    IReadOnlyList<ExportedListingBody> Listings,
    IReadOnlyList<ExportedRequestBody> Requests,
    IReadOnlyList<ExportedMessageBody> Messages);
