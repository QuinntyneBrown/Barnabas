using Barnabas.Domain.Requests;

namespace Barnabas.Application.Requests.DeclineRequest;

/// <summary>The decision, so the screen can show the request as settled.</summary>
public sealed record DeclineRequestResult(Guid RequestId, RequestStatus Status);
