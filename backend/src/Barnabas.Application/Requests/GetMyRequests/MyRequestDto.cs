using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;

namespace Barnabas.Application.Requests.GetMyRequests;

/// <summary>
/// One request the caller made, and what became of it.
/// </summary>
/// <remarks>
/// <see cref="ThreadId"/> is populated only for an accepted request, and that nullability is the
/// design rather than an omission. A pending request has no thread because nothing has been
/// decided; a declined one has none because declining opens none. The screen therefore decides
/// what to offer from the row in front of it rather than by asking a second question.
/// </remarks>
public sealed record MyRequestDto(
    Guid RequestId,
    Guid ListingId,
    string ListingTitle,
    ListingKind Kind,
    string OwnerDisplayName,
    string Neighbourhood,
    string Terms,
    RequestStatus Status,
    DateTimeOffset MadeAt,
    Guid? ThreadId);
