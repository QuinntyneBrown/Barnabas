using MediatR;

namespace Barnabas.Application.Requests.GetIncomingRequests;

/// <summary>
/// Asks for the requests made against the caller's listings.
/// </summary>
/// <remarks>
/// Empty on purpose. The caller comes from the session, so there is no parameter to change in
/// order to read another member's inbox.
/// </remarks>
public sealed record GetIncomingRequestsQuery : IRequest<IReadOnlyList<IncomingRequestDto>>;
