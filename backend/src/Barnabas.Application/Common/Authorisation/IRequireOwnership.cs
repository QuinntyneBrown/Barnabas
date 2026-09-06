namespace Barnabas.Application.Common.Authorisation;

/// <summary>
/// Declares that only the member who owns a resource may issue this request.
/// </summary>
/// <typeparam name="TResource">The owning entity to load and compare against the caller.</typeparam>
/// <remarks>
/// A request declares what it demands; it does not perform the check. The check runs in
/// <c>AuthorisationBehaviour</c>, so a handler cannot forget it and a handler that is
/// reached has already been authorised.
/// </remarks>
public interface IRequireOwnership<TResource>
    where TResource : class, Barnabas.Domain.Common.IOwnedResource
{
    /// <summary>The identifier of the resource whose owner may issue this request.</summary>
    Guid ResourceId { get; }
}
