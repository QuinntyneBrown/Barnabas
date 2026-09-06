using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Requests.Common;
using MediatR;

namespace Barnabas.Application.Requests.AcceptRequest;

/// <summary>
/// The affirmative decision, and the hinge of the whole product.
/// </summary>
/// <remarks>
/// The deciding member is taken from the session; what this carries is the request. Ownership is
/// declared rather than checked here, and it is ownership of the listing rather than of the
/// request - see <see cref="RequestOwnership"/>.
/// </remarks>
public sealed record AcceptRequestCommand(Guid RequestId)
    : IRequest<AcceptRequestResult>, IRequireOwnership<RequestOwnership>
{
    public Guid ResourceId => RequestId;
}
