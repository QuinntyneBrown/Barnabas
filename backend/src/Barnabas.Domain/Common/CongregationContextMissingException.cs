namespace Barnabas.Domain.Common;

/// <summary>
/// Raised when a congregation-owned entity is queried with no congregation in scope.
/// </summary>
/// <remarks>
/// The alternative — treating an absent congregation as "no filter" — would turn every
/// anonymous endpoint into a cross-tenant read. Failing closed is the point.
/// </remarks>
public sealed class CongregationContextMissingException : Exception
{
    public CongregationContextMissingException(string entityName)
        : base($"No congregation is in scope, so '{entityName}' cannot be queried.")
        => EntityName = entityName;

    public string EntityName { get; }
}
