using Barnabas.Domain.Requests;

namespace Barnabas.Application.Messaging.GetThread;

/// <summary>
/// One conversation, with what it is about.
/// </summary>
/// <remarks>
/// The listing and the standing of the request travel with the messages because the thread is
/// not a conversation in the abstract - it exists because somebody asked for something, and a
/// reader six weeks later needs to see what.
/// </remarks>
public sealed record ThreadDetailDto(
    Guid ThreadId,
    Guid ListingId,
    string ListingTitle,
    Guid OtherMemberId,
    string OtherMemberDisplayName,
    RequestStatus RequestStatus,
    IReadOnlyList<MessageDto> Messages);
