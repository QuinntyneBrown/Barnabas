using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.DeleteListing;

/// <summary>
/// Removes the listing, its requests, and the conversations they opened, in one unit of work.
/// </summary>
/// <remarks>
/// The cascade is written out rather than left to the schema. Only messages and read marks
/// cascade from a thread; requests and threads hang off the listing by identifier alone, so
/// deleting the listing without them would leave a thread that leads nowhere and a request
/// against something that no longer exists - which L2-064, requiring every thread to carry its
/// listing, does not allow.
/// <para>
/// Only from the archive. An active listing is refused, so the member has taken it off the board
/// as a separate, undoable act before this one.
/// </para>
/// </remarks>
public sealed class DeleteListingCommandHandler : IRequestHandler<DeleteListingCommand>
{
    private readonly IBarnabasDbContext _context;

    public DeleteListingCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        if (listing.IsActive)
        {
            throw new ListingStillOnTheBoardException(listing.Id);
        }

        var requests = await _context.ListingRequests
            .Where(listingRequest => listingRequest.ListingId == listing.Id)
            .ToListAsync(cancellationToken);

        var requestIds = requests.ConvertAll(listingRequest => listingRequest.Id);

        var threads = await _context.MessageThreads
            .Where(thread => requestIds.Contains(thread.RequestId))
            .ToListAsync(cancellationToken);

        // Messages and read marks cascade from the thread, so removing the threads is enough for
        // those two and naming them here would be repeating what the schema already guarantees.
        _context.MessageThreads.RemoveRange(threads);
        _context.ListingRequests.RemoveRange(requests);
        _context.Listings.Remove(listing);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
