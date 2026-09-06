using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Requests.Common;
using MediatR;

namespace Barnabas.Application.Requests.DeclineRequest;

/// <summary>
/// The negative decision.
/// </summary>
/// <remarks>
/// It carries no reason, deliberately. A reason would be shown to the requester, and a private
/// message in a thread serves that far better than a field filled in under the pressure of
/// saying no. If one is ever wanted, this is where it belongs.
/// </remarks>
public sealed record DeclineRequestCommand(Guid RequestId)
    : IRequest<DeclineRequestResult>, IRequireOwnership<RequestOwnership>
{
    public Guid ResourceId => RequestId;
}
