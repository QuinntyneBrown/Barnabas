using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class OwnerLookup<TResource> : IOwnerLookup<TResource>
    where TResource : class, IOwnedResource
{
    private readonly BarnabasDbContext _context;

    public OwnerLookup(BarnabasDbContext context) => _context = context;

    /// <inheritdoc />
    /// <remarks>
    /// The read goes through the congregation-filtered set, so a resource in another
    /// congregation is already absent before ownership is considered. That is what makes the
    /// answer 404 rather than 403 for a cross-congregation identifier, per L2-089.
    /// </remarks>
    public Task<TResource?> FindAsync(Guid resourceId, CancellationToken cancellationToken) =>
        _context.Set<TResource>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);
}
