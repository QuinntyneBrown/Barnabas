using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Members.LeaveCongregation;

/// <summary>
/// The caller leaves.
/// </summary>
/// <remarks>
/// It names no member, so there is no identifier here that could remove somebody else. Reachable
/// by a member still waiting on a moderator: somebody who has changed their mind about joining
/// should not have to be approved first in order to withdraw.
/// </remarks>
public sealed record LeaveCongregationCommand : IRequest, IAllowUnapproved;
