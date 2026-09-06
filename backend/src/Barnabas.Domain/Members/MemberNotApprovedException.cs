namespace Barnabas.Domain.Members;

/// <summary>Raised when a role is granted to a member who has not been let in yet.</summary>
public sealed class MemberNotApprovedException : Exception
{
    public MemberNotApprovedException(Guid memberId)
        : base("That member has not been approved yet.") => MemberId = memberId;

    public Guid MemberId { get; }
}
