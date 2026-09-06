namespace Barnabas.Application.Congregations.ConfigureCongregation;

public sealed record ConfiguredCongregationResult(
    Guid CongregationId,
    string Name,
    string Slug,
    IReadOnlyList<string> Neighbourhoods);
