using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Requests.DeclineRequest;

/// <summary>
/// Declines the request.
/// </summary>
/// <remarks>
/// A state change rather than a deletion. The request is retained so the requester can discover
/// what became of their ask, which is the whole reason declining is a decision the product
/// records rather than silence.
/// <para>
/// No thread is opened on this path, and there is nothing here that could open one.
/// </para>
/// </remarks>
public sealed class DeclineRequestCommandHandler : IRequestHandler<DeclineRequestCommand, DeclineRequestResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly TimeProvider _time;

    public DeclineRequestCommandHandler(IBarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task<DeclineRequestResult> Handle(DeclineRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingRequest = await _context.ListingRequests
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException();

        listingRequest.Decline(_time.GetUtcNow());

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
