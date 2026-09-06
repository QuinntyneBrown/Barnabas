using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.ArchiveListing;

/// <summary>Shelves the listing and commits.</summary>
public sealed class ArchiveListingCommandHandler
    : IRequestHandler<ArchiveListingCommand, ArchivedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly TimeProvider _time;

    public ArchiveListingCommandHandler(IBarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task<ArchivedListingResult> Handle(
        ArchiveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        listing.Archive(_time.GetUtcNow());

        await _context.SaveChangesAsync(cancellationToken);

        return new ArchivedListingResult(listing.Id, listing.Status);
    }
}
