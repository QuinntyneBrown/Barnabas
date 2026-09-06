using Barnabas.Domain.Notifications;

namespace Barnabas.Application.Notifications.Preferences;

/// <summary>One kind, and whether the member wants to hear about it.</summary>
public sealed record NotificationPreferenceDto(NotificationKind Kind, bool Enabled);
