using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Messaging.GetThread;
using Barnabas.Application.Notifications.Common;
using Barnabas.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Messaging.SendMessage;

/// <summary>
/// Confirms the caller is a party and appends.
/// </summary>
/// <remarks>
/// The appending is the entity's own, so both this and the read ask the thread who may touch it
/// rather than each deciding for themselves.
/// </remarks>
public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public SendMessageCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        INotifier notifier,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _notifier = notifier;
        _time = time;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caller = _congregation.MemberId;

        var thread = await _context.MessageThreads
            .Include(thread => thread.Messages)
            .FirstOrDefaultAsync(thread => thread.Id == request.ThreadId, cancellationToken)
            ?? throw new NotFoundException();

        // Raises if the caller is not one of the two, and the API reports that as not found.
        var message = thread.Append(Guid.NewGuid(), caller, request.Body, _time.GetUtcNow());

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == thread.ListingId, cancellationToken);

        // The other party, and only them. The thread knows which of its two members that is, so
        // L2-072 is settled by asking it rather than by filtering a list afterwards.
        await _notifier.MessageSentAsync(
            thread.Id,
            thread.ListingId,
            listing?.Title ?? string.Empty,
            caller,
            thread.OtherParty(caller),
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var sender = await _context.Members
            .FirstOrDefaultAsync(member => member.Id == caller, cancellationToken);

        return new MessageDto(
            message.Id,
            message.SenderId,
            sender?.DisplayName ?? string.Empty,
            message.Body,
            message.SentAt,
            SentByCaller: true);
    }
}
