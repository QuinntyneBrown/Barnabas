using Barnabas.Domain.Common;

namespace Barnabas.Application.Requests.Common;

/// <summary>
/// Who may decide a request: the owner of the listing it was made against.
/// </summary>
/// <remarks>
/// A request is not itself an owned resource. It belongs to the member who made it, but the
/// member who may act on it is the one who owns the listing - so "owner" means something
/// different here than it does anywhere else, and conflating the two would let a requester
/// accept their own request.
/// <para>
/// Naming that relation as a type is what lets accept and decline declare
/// <c>IRequireOwnership</c> and have the pipeline enforce it, rather than each handler
/// remembering to compare two identifiers it had to load first.
/// </para>
/// </remarks>
public sealed class RequestOwnership : IOwnedResource
{
    public RequestOwnership(Guid requestId, Guid ownerId)
    {
        Id = requestId;
        OwnerId = ownerId;
    }

    /// <summary>The request's own identifier.</summary>
    public Guid Id { get; }

    /// <summary>The owner of the listing the request was made against.</summary>
    public Guid OwnerId { get; }
}
