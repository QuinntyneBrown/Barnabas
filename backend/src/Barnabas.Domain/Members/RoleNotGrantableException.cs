namespace Barnabas.Domain.Members;

/// <summary>
/// Raised when a role is granted that may not be granted through the product.
/// </summary>
/// <remarks>
/// Administrator is provisioned, never promoted to. A moderator able to grant it could make
/// themselves one, and the role gate would then be a formality.
/// </remarks>
public sealed class RoleNotGrantableException : Exception
{
    public RoleNotGrantableException(Guid memberId, MemberRole role)
        : base("That role cannot be granted here.")
    {
        MemberId = memberId;
        Role = role;
    }

    public Guid MemberId { get; }

    public MemberRole Role { get; }
}
