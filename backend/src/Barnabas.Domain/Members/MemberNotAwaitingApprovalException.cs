namespace Barnabas.Domain.Members;

/// <summary>Raised when a decision is made about a member who is not waiting for one.</summary>
public sealed class MemberNotAwaitingApprovalException : Exception
{
    public MemberNotAwaitingApprovalException(Guid memberId)
        : base("That member is not waiting to be approved.") => MemberId = memberId;

    public Guid MemberId { get; }
}
