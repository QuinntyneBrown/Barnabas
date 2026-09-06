using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Requests.Common;
using Barnabas.Application.Requests.MakeLoanRequest;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using FluentValidation;
using FluentValidation.Results;
using Barnabas.Application.Notifications.Common;
using MediatR;

namespace Barnabas.Application.Requests.MakeHelpRequest;

/// <summary>Creates the request against a declared window, and commits.</summary>
public sealed class MakeHelpRequestCommandHandler : IRequestHandler<MakeHelpRequestCommand, MadeRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly INotifier _notifier;
    private readonly TimeProvider _time;

    public MakeHelpRequestCommandHandler(
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

    public async Task<MadeRequestResult> Handle(MakeHelpRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requester = _congregation.MemberId;

        var listing = await RequestableListing.LoadAsync(
            _context, request.ListingId, requester, ListingKind.Help, cancellationToken);

        // The window has to be one this listing offered. Only the listing can answer that, which
        // is why the check is here and not in the validator - and it is a 400 rather than a 404
        // because the listing was found; it is the choice against it that is wrong.
        if (!listing.DeclaresWindow(request.AvailabilityWindowId!.Value))
        {
            // A validation failure that needed the listing to detect, so it is raised here and
            // not in the validator - but it is the same kind of failure and reaches the caller
            // through the same contract, naming the field they got wrong.
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(MakeHelpRequestCommand.AvailabilityWindowId),
                    "Choose one of the windows this offer declared."),
            ]);
        }

        var made = ListingRequest.MakeHelpRequest(
            Guid.NewGuid(),
            _congregation.CongregationId,
            listing.Id,
            requester,
            request.Message,
            request.AvailabilityWindowId.Value,
            _time.GetUtcNow());

        _context.ListingRequests.Add(made);

        // Staged into the same unit of work, so a request the filtered index refuses leaves no
        // notification behind - L2-070.
        await _notifier.RequestMadeAsync(listing, made, cancellationToken);

        await RequestableListing.SaveOrDuplicateAsync(_context, cancellationToken);

        return new MadeRequestResult(made.Id);
    }
}
