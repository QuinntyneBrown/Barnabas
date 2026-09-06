namespace Barnabas.Domain.Members;

public enum MemberStatus
{
    AwaitingApproval = 0,
    Approved = 1,
    Declined = 2,

    /// <summary>
    /// They left of their own accord.
    /// </summary>
    /// <remarks>
    /// Distinct from declined, which is a moderator's decision. A departed member cannot sign in
    /// and appears in no directory, but the record stays so their old messages remain legible to
    /// the members they spoke to.
    /// </remarks>
    Left = 3,
}
