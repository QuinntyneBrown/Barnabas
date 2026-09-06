using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.CloseOutListing;

/// <summary>
/// Applies the close-out and commits.
/// </summary>
/// <remarks>
/// There is no ownership check here. The command declares that it demands one, and the
/// authorisation behaviour has already made it - so a handler that is reached has been
/// authorised, and cannot forget to be.
/// </remarks>
public sealed class CloseOutListingCommandHandler : IRequestHandler<CloseOutListingCommand, CloseOutListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly TimeProvider _time;

    public CloseOutListingCommandHandler(IBarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task<CloseOutListingResult> Handle(
        CloseOutListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        // Raises if it has already been closed out, which the API reports as a conflict.
        listing.CloseOut(_time.GetUtcNow());

        await _context.SaveChangesAsync(cancellationToken);

        return new CloseOutListingResult(listing.Id, listing.Status);
    }
}
