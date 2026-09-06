using MediatR;

namespace Barnabas.Application.Notifications.GetNotifications;

/// <summary>
/// How many notifications the caller has not read.
/// </summary>
/// <remarks>
/// Asked from every screen at every width, so it is the most-called query in the product — which
/// is why a filtered index exists for exactly this predicate.
/// </remarks>
public sealed record GetUnreadCountQuery : IRequest<UnreadNotificationCount>;
