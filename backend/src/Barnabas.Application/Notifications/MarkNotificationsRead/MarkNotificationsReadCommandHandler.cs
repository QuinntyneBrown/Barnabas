using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Notifications.MarkNotificationsRead;

/// <summary>Marks them read and commits.</summary>
public sealed class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public MarkNotificationsReadCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recipient = _congregation.MemberId;
        var now = _time.GetUtcNow();

        // Always narrowed to the caller's own, so an identifier belonging to somebody else simply
        // matches nothing rather than being refused - there is no resource to confirm.
        var notifications = await _context.Notifications
            .Where(notification =>
                notification.RecipientId == recipient
                && notification.ReadAt == null
                && (request.NotificationId == null || notification.Id == request.NotificationId))
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkRead(now);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
