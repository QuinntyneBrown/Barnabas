namespace Barnabas.Api.Middleware;

/// <summary>
/// Raises the body limit for one endpoint.
/// </summary>
/// <remarks>
/// Two requirements pull in opposite directions here, and this is what settles them.
/// <c>L2-096 AC2</c> requires an oversized JSON body to be refused at 1 MB; <c>L2-032 AC1</c>
/// requires a 2 MB JPEG to be accepted. One number cannot do both, so the limit is a property of
/// the endpoint rather than of the API — 1 MB everywhere, and more only where a route says so.
/// <para>
/// Read as endpoint metadata rather than checked against a path, so the exception is declared
/// beside the action it applies to and cannot drift when a route is renamed.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MaxRequestBodyAttribute : Attribute
{
    public MaxRequestBodyAttribute(long bytes) => Bytes = bytes;

    public long Bytes { get; }
}
