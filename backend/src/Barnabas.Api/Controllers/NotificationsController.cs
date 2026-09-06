using Barnabas.Application.Notifications.GetNotifications;
using Barnabas.Application.Notifications.MarkNotificationsRead;
using Barnabas.Application.Notifications.Preferences;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// What has happened that concerns the caller.
/// </summary>
/// <remarks>
/// Nothing here names a member. The recipient comes from the verified session, so there is no
/// identifier a caller could change in order to read, clear, or silence somebody else's.
/// </remarks>
[ApiController]
[Route("notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Mine(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetNotificationsQuery(), cancellationToken));

    /// <summary>Asked from every screen at every width, which is why it is its own endpoint.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadNotificationCount>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationCount>> Unread(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetUnreadCountQuery(), cancellationToken));

    /// <summary>Marks one read, or all of them when no identifier is given.</summary>
    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Read(
        [FromQuery] Guid? notificationId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkNotificationsReadCommand(notificationId), cancellationToken);

        return NoContent();
    }

    [HttpGet("preferences")]
    [ProducesResponseType<IReadOnlyList<NotificationPreferenceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceDto>>> Preferences(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetNotificationPreferencesQuery(), cancellationToken));

    [HttpPut("preferences")]
    [ProducesResponseType<IReadOnlyList<NotificationPreferenceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceDto>>> SetPreferences(
        SetNotificationPreferencesCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(command, cancellationToken));
}
