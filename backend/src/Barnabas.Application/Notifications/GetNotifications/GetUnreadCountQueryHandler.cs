using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.GetNotifications;

/// <summary>Counts what the caller has not read.</summary>
public sealed class GetUnreadCountQueryHandler
    : IRequestHandler<GetUnreadCountQuery, UnreadNotificationCount>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetUnreadCountQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<UnreadNotificationCount> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        var recipient = _congregation.MemberId;

        var unread = await _context.Notifications
            .CountAsync(
                notification => notification.RecipientId == recipient && notification.ReadAt == null,
                cancellationToken);

        return new UnreadNotificationCount(unread);
    }
}
