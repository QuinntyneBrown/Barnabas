using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Notifications.Common;
using Barnabas.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.DeclineRequest;

/// <summary>
/// Records the owner's refusal.
/// </summary>
/// <remarks>
/// The request is kept rather than removed. A requester who is told nothing cannot tell a refusal
/// from a message that never arrived, and the row is what lets them ask again for something else.
/// <para>
/// The listing is loaded because the notification names it. A refusal that said only "declined"
/// would leave the requester working out which of their asks it was about.
/// </para>
/// </remarks>
public sealed class DeclineRequestCommandHandler : IRequestHandler<DeclineRequestCommand, DeclineRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public DeclineRequestCommandHandler(
        IBarnabasDbContext context,
        INotifier notifier,
        TimeProvider time)
    {
        _context = context;
        _notifier = notifier;
        _time = time;
    }

    public async Task<DeclineRequestResult> Handle(
        DeclineRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingRequest = await _context.ListingRequests
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException();

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == listingRequest.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        listingRequest.Decline(_time.GetUtcNow());

        // Staged before the save, so a decision the row version refuses leaves no notification
        // behind. No thread, because a decline opens none - L2-071.
        await _notifier.RequestDecidedAsync(listing, listingRequest, threadId: null, accepted: false, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RequestAlreadyDecidedException(listingRequest.Id, listingRequest.Status);
        }

        return new DeclineRequestResult(listingRequest.Id, listingRequest.Status);
    }
}
