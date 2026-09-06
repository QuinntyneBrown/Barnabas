using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Notifications.Preferences;

/// <summary>
/// What the caller has chosen to hear about.
/// </summary>
/// <remarks>
/// Every configurable kind comes back, whether or not a row exists for it, because a settings
/// screen has to show a switch for each one — and absence means enabled rather than absent.
/// </remarks>
public sealed record GetNotificationPreferencesQuery
    : IRequest<IReadOnlyList<NotificationPreferenceDto>>, IAllowUnapproved;
