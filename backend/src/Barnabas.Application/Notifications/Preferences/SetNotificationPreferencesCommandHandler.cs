using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.Preferences;

/// <summary>
/// Records the choices and commits.
/// </summary>
/// <remarks>
/// Only the configurable kinds are stored. A member cannot switch off being told that a moderator
/// removed their listing, and accepting the row would let them think they had.
/// </remarks>
public sealed class SetNotificationPreferencesCommandHandler
    : IRequestHandler<SetNotificationPreferencesCommand, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public SetNotificationPreferencesCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        SetNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memberId = _congregation.MemberId;
        var congregationId = _congregation.CongregationId;

        var existing = await _context.NotificationPreferences
            .Where(preference => preference.MemberId == memberId)
            .ToListAsync(cancellationToken);

        foreach (var choice in request.Preferences ?? [])
        {
            if (!NotificationPreferences.Configurable.Contains(choice.Kind))
            {
                continue;
            }

            var held = existing.Find(preference => preference.Kind == choice.Kind);

            if (held is null)
            {
                _context.NotificationPreferences.Add(
                    new NotificationPreference(congregationId, memberId, choice.Kind, choice.Enabled));
            }
            else
            {
                held.Set(choice.Enabled);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

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
