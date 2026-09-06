namespace Barnabas.Application.Messaging.GetMyThreads;

/// <summary>
/// One conversation, as a row in the member's messages.
/// </summary>
/// <remarks>
/// <see cref="Unread"/> is computed for the caller rather than stored on the thread. Two members
/// looking at the same conversation can legitimately disagree about whether it is unread, and a
/// single flag on the thread could only ever be right for one of them.
/// </remarks>
public sealed record ThreadSummaryDto(
    Guid ThreadId,
    Guid OtherMemberId,
    string OtherMemberDisplayName,
    Guid ListingId,
    string ListingTitle,
    string LatestMessage,
    DateTimeOffset? LatestAt,
    bool Unread);
