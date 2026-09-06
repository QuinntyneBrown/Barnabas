using MediatR;

namespace Barnabas.Application.Congregations.GetCongregation;

/// <summary>
/// The caller's own congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The identifier comes from the verified session, so there is nothing
/// here to change in order to read another parish's name or its neighbourhoods.
/// </remarks>
public sealed record GetCongregationQuery : IRequest<CongregationDto>;
