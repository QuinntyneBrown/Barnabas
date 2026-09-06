using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Messaging;
using Barnabas.Domain.Requests;
using Barnabas.Application.Notifications.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.AcceptRequest;

/// <summary>
/// Accepts the request and opens the thread, together.
/// </summary>
/// <remarks>
/// Both writes go in one call to save, so they are one transaction. A committed acceptance
/// without a thread would leave the requester notified of a decision they cannot act on.
/// <para>
/// Three mechanisms keep "one decision, one thread" true, and only the last two are load-bearing.
/// The entity refuses a second decision, which produces a clear error in the ordinary case. The
/// row version decides an accept racing a decline. The unique constraint on the thread's request
/// decides the thread. A status check made before the save cannot promise either of the last two.
/// </para>
/// </remarks>
public sealed class AcceptRequestCommandHandler : IRequestHandler<AcceptRequestCommand, AcceptRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public AcceptRequestCommandHandler(
        IBarnabasDbContext context,
        INotifier notifier,
        TimeProvider time)
    {
        _context = context;
        _notifier = notifier;
        _time = time;
    }

    public async Task<AcceptRequestResult> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingRequest = await _context.ListingRequests
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException();

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == listingRequest.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        var now = _time.GetUtcNow();

        // Raises if the request already holds a decision.
        listingRequest.Accept(now);

        var thread = MessageThread.OpenFor(
            Guid.NewGuid(),
            listing.CongregationId,
            listingRequest.Id,
            listing.Id,
            listing.OwnerId,
            listingRequest.RequesterId,
            now);

        _context.MessageThreads.Add(thread);

        // Staged before the save, so a decision the row version refuses leaves no notification
        // behind. The thread travels with it, because an acceptance leads somewhere - L2-071.
        await _notifier.RequestDecidedAsync(listing, listingRequest, thread.Id, accepted: true, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another decision committed between the load and the save. Exactly one survives,
            // and this caller is told the request was already decided.
            throw new RequestAlreadyDecidedException(listingRequest.Id, listingRequest.Status);
        }
        catch (DbUpdateException)
        {
            // The thread's unique request refused a second one.
            throw new RequestAlreadyDecidedException(listingRequest.Id, listingRequest.Status);
        }

        return new AcceptRequestResult(listingRequest.Id, thread.Id);
    }
}
