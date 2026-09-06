namespace Barnabas.Application.Common.Exceptions;

/// <summary>
/// Raised when a request names a resource the caller cannot see.
/// </summary>
/// <remarks>
/// "Cannot see" deliberately conflates two situations. The resource may not exist at all, or
/// it may belong to another congregation and have been removed by the query filter before the
/// handler looked. <c>L2-089</c> requires those two answers to be indistinguishable, so they
/// are raised as the same exception and rendered as the same 404. A distinct "forbidden"
/// answer for the second case would confirm the resource exists, which is itself a disclosure.
/// </remarks>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : base("The resource was not found.")
    {
    }
}
