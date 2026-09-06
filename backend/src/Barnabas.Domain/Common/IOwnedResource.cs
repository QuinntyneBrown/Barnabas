namespace Barnabas.Domain.Common;

/// <summary>
/// Marks an entity that one member owns, and whose ownership decides who may modify it.
/// </summary>
public interface IOwnedResource
{
    Guid Id { get; }

    Guid OwnerId { get; }
}
