using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Access.RefreshSession;

/// <summary>
/// Renews a session without another trip to the member's inbox.
/// </summary>
/// <remarks>
/// The token arrives in a cookie rather than a field, so the controller builds this from what
/// the browser sent rather than from a body the page could have written.
/// </remarks>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<SessionResult>, IAllowUnapproved;
