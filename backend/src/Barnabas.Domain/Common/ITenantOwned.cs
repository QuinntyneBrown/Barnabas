namespace Barnabas.Domain.Common;

/// <summary>
/// Marks an entity as belonging to exactly one congregation.
/// </summary>
/// <remarks>
/// Implementing this is the whole of what an entity does to come under the
/// congregation query filter. The filter is applied by reflection over the model,
/// so there is no registration to remember and no way for a handler to opt out.
/// </remarks>
public interface ITenantOwned
{
    Guid CongregationId { get; }
}
