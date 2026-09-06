using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.Preferences;

/// <summary>Reads the caller's choices, filling in the default where they have made none.</summary>
public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetNotificationPreferencesQueryHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var memberId = _congregation.MemberId;

        var chosen = await _context.NotificationPreferences
            .Where(preference => preference.MemberId == memberId)
            .ToDictionaryAsync(
                preference => preference.Kind,
                preference => preference.Enabled,
                cancellationToken);

        var preferences = new NotificationPreferences(chosen);

        return [.. NotificationPreferences.Configurable
            .OrderBy(kind => kind)
            .Select(kind => new NotificationPreferenceDto(kind, preferences.Allows(kind)))];
    }
}
