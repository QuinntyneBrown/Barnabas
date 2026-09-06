using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.RestoreListing;

/// <summary>Puts it back and commits.</summary>
public sealed class RestoreListingCommandHandler
    : IRequestHandler<RestoreListingCommand, RestoredListingResult>
{
    private readonly IBarnabasDbContext _context;

    public RestoreListingCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task<RestoredListingResult> Handle(
        RestoreListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        listing.Restore();

        await _context.SaveChangesAsync(cancellationToken);

        return new RestoredListingResult(listing.Id, listing.Status);
    }
}
