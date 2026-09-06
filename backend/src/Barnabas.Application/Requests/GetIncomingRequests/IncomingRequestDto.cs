using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;

namespace Barnabas.Application.Requests.GetIncomingRequests;

/// <summary>
/// One request made against a listing the caller owns.
/// </summary>
/// <remarks>
/// The message is here because it is the whole basis for the decision, and the terms because a
/// loan is decided on when the thing comes back. A row showing only who asked would send the
/// owner somewhere else to find out what they were agreeing to.
/// </remarks>
public sealed record IncomingRequestDto(
    Guid RequestId,
    Guid RequesterId,
    string RequesterDisplayName,
    Guid ListingId,
    string ListingTitle,
    ListingKind Kind,
    string Message,
    string Terms,
    RequestStatus Status,
    DateTimeOffset MadeAt);
