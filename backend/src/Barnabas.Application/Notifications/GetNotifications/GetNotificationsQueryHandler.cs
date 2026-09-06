using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.GetNotifications;

/// <summary>Reads the caller's notifications, newest first.</summary>
public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private const int MostRecent = 50;

    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetNotificationsQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var recipient = _congregation.MemberId;

        return await _context.Notifications
            .Where(notification => notification.RecipientId == recipient)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(MostRecent)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Kind,
                notification.SubjectMemberId,
                notification.SubjectMemberDisplayName,
                notification.ListingId,
                notification.ListingTitle,
                notification.RequestId,
                notification.ThreadId,
                notification.CreatedAt,
                notification.ReadAt == null))
            .ToListAsync(cancellationToken);
    }
}
