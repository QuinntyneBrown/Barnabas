using MediatR;

namespace Barnabas.Application.Notifications.MarkNotificationsRead;

/// <summary>
/// Marks one notification read, or all of them.
/// </summary>
/// <remarks>
/// One command rather than two, because the difference is a single identifier and the rule about
/// whose notifications may be marked is the same either way. A null identifier means all of the
/// caller's — never anybody else's, because the recipient comes from the session.
/// </remarks>
public sealed record MarkNotificationsReadCommand(Guid? NotificationId) : IRequest;
