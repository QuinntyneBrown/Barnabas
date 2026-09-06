using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Barnabas.Application.Notifications.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.MakeLoanRequest;

/// <summary>
/// Applies the eligibility policy and creates the request.
/// </summary>
/// <remarks>
/// The duplicate rule is enforced twice, and needs to be. The policy check gives a clear 409 in
/// the ordinary case without a second round trip; the filtered unique index decides it under a
/// race, because two callers can both pass the check before either commits. A policy alone
/// cannot make a rule true under concurrency, and only the constraint can.
/// </remarks>
public sealed class MakeLoanRequestCommandHandler : IRequestHandler<MakeLoanRequestCommand, MadeRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public MakeLoanRequestCommandHandler(
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

    public async Task<MadeRequestResult> Handle(MakeLoanRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requester = _congregation.MemberId;

        // Read through the filtered set, so a listing in another congregation is already absent
        // and the answer is not found rather than anything that confirms it exists.
        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        var eligibility = RequestEligibilityPolicy.Check(listing, requester, ListingKind.Lend);

        if (eligibility != RequestEligibility.Eligible)
        {
            throw new RequestNotEligibleException(eligibility);
        }

        var alreadyOpen = await _context.ListingRequests.AnyAsync(
            existing => existing.ListingId == listing.Id
                && existing.RequesterId == requester
                && existing.Status == RequestStatus.Pending,
            cancellationToken);

        if (alreadyOpen)
        {
            throw new RequestNotEligibleException(RequestEligibility.DuplicateRequest);
        }

        var made = ListingRequest.MakeLoanRequest(
            Guid.NewGuid(),
            _congregation.CongregationId,
            listing.Id,
            requester,
            request.Message,
            request.PickupOn!.Value,
            request.ReturnBy!.Value,
            _time.GetUtcNow());

        _context.ListingRequests.Add(made);

        // Staged into the same unit of work, so a request the filtered index refuses leaves no
        // notification behind - L2-070.
        await _notifier.RequestMadeAsync(listing, made, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The index refused it, which means another request of this member's got there
            // first. The answer is the same one the check above would have given.
            throw new RequestNotEligibleException(RequestEligibility.DuplicateRequest);
        }

        return new MadeRequestResult(made.Id);
    }
}
