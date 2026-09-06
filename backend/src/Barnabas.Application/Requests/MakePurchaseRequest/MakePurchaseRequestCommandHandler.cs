using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Requests.Common;
using Barnabas.Application.Requests.MakeLoanRequest;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Barnabas.Application.Notifications.Common;
using MediatR;

namespace Barnabas.Application.Requests.MakePurchaseRequest;

/// <summary>Creates the request and commits.</summary>
public sealed class MakePurchaseRequestCommandHandler : IRequestHandler<MakePurchaseRequestCommand, MadeRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public MakePurchaseRequestCommandHandler(
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

    public async Task<MadeRequestResult> Handle(MakePurchaseRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requester = _congregation.MemberId;

        var listing = await RequestableListing.LoadAsync(
            _context, request.ListingId, requester, ListingKind.Sell, cancellationToken);

        var made = ListingRequest.MakePurchaseRequest(
            Guid.NewGuid(),
            _congregation.CongregationId,
            listing.Id,
            requester,
            request.Message,
            request.PickupAt!.Value,
            _time.GetUtcNow());

        _context.ListingRequests.Add(made);

        // Staged into the same unit of work, so a request the filtered index refuses leaves no
        // notification behind - L2-070.
        await _notifier.RequestMadeAsync(listing, made, cancellationToken);

        await RequestableListing.SaveOrDuplicateAsync(_context, cancellationToken);

        return new MadeRequestResult(made.Id);
    }
}
