using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Notifications.Preferences;

/// <summary>
/// Chooses which kinds the caller hears about.
/// </summary>
/// <remarks>
/// It names no member. A caller who could set somebody else's preferences could silence them.
/// </remarks>
public sealed record SetNotificationPreferencesCommand(
    IReadOnlyList<NotificationPreferenceDto>? Preferences)
    : IRequest<IReadOnlyList<NotificationPreferenceDto>>, IAllowUnapproved;
