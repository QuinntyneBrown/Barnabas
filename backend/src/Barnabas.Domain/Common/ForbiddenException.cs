namespace Barnabas.Domain.Common;

/// <summary>
/// Raised when an identified member may not perform an action on a resource.
/// </summary>
/// <remarks>
/// Carries no detail about the resource. Mapped to 403 by the API.
/// A resource in another congregation never reaches this exception — the query
/// filter removes it first, so the answer is 404 and existence stays unprobeable.
/// </remarks>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
        : base("The caller may not perform this action.")
    {
    }
}
