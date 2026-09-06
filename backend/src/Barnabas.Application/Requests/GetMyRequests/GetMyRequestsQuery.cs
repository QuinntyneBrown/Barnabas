using Barnabas.Domain.Requests;
using MediatR;

namespace Barnabas.Application.Requests.GetMyRequests;

/// <summary>Asks for the requests the caller made, optionally of one standing.</summary>
public sealed record GetMyRequestsQuery(RequestStatus? Status) : IRequest<IReadOnlyList<MyRequestDto>>;
