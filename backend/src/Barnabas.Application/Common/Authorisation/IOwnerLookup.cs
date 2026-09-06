using Barnabas.Domain.Common;

namespace Barnabas.Application.Common.Authorisation;

/// <summary>
/// Loads an owned resource so the authorisation behaviour can compare its owner against
/// the caller.
/// </summary>
/// <remarks>
/// The lookup goes through the congregation-filtered context, so a resource in another
/// congregation is already absent before ownership is considered. That is what makes the
/// answer 404 rather than 403 for a cross-congregation identifier, per L2-089.
/// </remarks>
public interface IOwnerLookup<TResource>
    where TResource : class, IOwnedResource
{
    Task<TResource?> FindAsync(Guid resourceId, CancellationToken cancellationToken);
}
