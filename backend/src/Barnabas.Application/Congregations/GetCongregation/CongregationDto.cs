namespace Barnabas.Application.Congregations.GetCongregation;

/// <summary>
/// A congregation as its own members see it.
/// </summary>
/// <remarks>
/// The name is what every screen identifying the board reads, so no screen hard-codes a parish.
/// The neighbourhoods are what a profile and a listing form offer, in the order the parish office
/// wrote them.
/// </remarks>
public sealed record CongregationDto(
    Guid CongregationId,
    string Name,
    string Slug,
    IReadOnlyList<string> Neighbourhoods);
