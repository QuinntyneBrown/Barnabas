using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Congregations.GetCongregation;

/// <summary>
/// The caller's own congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The identifier comes from the verified session, so there is nothing
/// here to change in order to read another parish's name or its neighbourhoods.
/// <para>
/// Reachable by a member still waiting on a moderator, because the screen that tells them so has
/// to name the congregation they are waiting on - <c>L2-011 AC2</c>.
/// </para>
/// </remarks>
public sealed record GetCongregationQuery : IRequest<CongregationDto>, IAllowUnapproved;
