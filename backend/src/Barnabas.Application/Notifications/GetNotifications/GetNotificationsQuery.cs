using MediatR;

namespace Barnabas.Application.Notifications.GetNotifications;

/// <summary>
/// The caller's own notifications, newest first.
/// </summary>
/// <remarks>
/// It names no member: the recipient comes from the session, so there is nothing here to change
/// in order to read somebody else's.
/// </remarks>
public sealed record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;
