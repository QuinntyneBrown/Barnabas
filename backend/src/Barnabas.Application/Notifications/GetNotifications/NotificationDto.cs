using Barnabas.Domain.Notifications;

namespace Barnabas.Application.Notifications.GetNotifications;

/// <summary>
/// One notification, and where it leads.
/// </summary>
/// <remarks>
/// The identifiers rather than a path. Where a thing lives is the client's business, and a URL
/// stored in a row would be wrong the first time a route changed — but every kind carries the
/// identifiers its destination needs, which is what makes <c>L2-074</c> checkable.
/// </remarks>
public sealed record NotificationDto(
    Guid NotificationId,
    NotificationKind Kind,
    Guid? SubjectMemberId,
    string? SubjectMemberDisplayName,
    Guid? ListingId,
    string? ListingTitle,
    Guid? RequestId,
    Guid? ThreadId,
    DateTimeOffset CreatedAt,
    bool Unread);
