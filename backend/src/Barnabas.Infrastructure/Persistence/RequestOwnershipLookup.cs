using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Requests.Common;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Finds who may decide a request: the owner of the listing it was made against.
/// </summary>
/// <remarks>
/// A join rather than a property read, because the answer lives on the listing. Both sides go
/// through the filtered sets, so a request in another congregation resolves to nothing and the
/// handler answers not found rather than forbidden.
/// </remarks>
public sealed class RequestOwnershipLookup : IOwnerLookup<RequestOwnership>
{
    private readonly BarnabasDbContext _context;

    public RequestOwnershipLookup(BarnabasDbContext context) => _context = context;

    public async Task<RequestOwnership?> FindAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        var owner = await _context.ListingRequests
            .Where(request => request.Id == resourceId)
            .Join(
                _context.Listings,
                request => request.ListingId,
                listing => listing.Id,
                (request, listing) => listing.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);

        return owner == Guid.Empty ? null : new RequestOwnership(resourceId, owner);
    }
}
