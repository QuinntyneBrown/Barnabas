using Barnabas.Domain.Common;

namespace Barnabas.Domain.Members;

/// <summary>
/// One person in one congregation. The group is a congregation or parish; never "users".
/// </summary>
/// <remarks>
/// Barnabas holds no password and no password hash. Identity is possession of the
/// mailbox, so there is nothing here to breach and no reset flow to protect.
/// </remarks>
public sealed class Member : ITenantOwned
{
    public const int DisplayNameMaxLength = 200;
    public const int ReasonForJoiningMaxLength = 500;

    private Member()
    {
    }

    public Member(
        Guid id,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood,
        MemberRole role = MemberRole.Member,
        MemberStatus status = MemberStatus.Approved)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        CongregationId = congregationId;
        EmailAddress = Normalise(emailAddress);
        DisplayName = displayName;
        Neighbourhood = neighbourhood;
        Role = role;
        Status = status;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    /// <summary>
    /// Stored lower-cased. Sign-in looks a member up by address with no congregation in
    /// scope, so one address maps to exactly one member across the whole deployment.
    /// </summary>
    public string EmailAddress { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Neighbourhood { get; private set; } = string.Empty;

    public MemberRole Role { get; private set; }

    public MemberStatus Status { get; private set; }

    public bool IsApproved => Status == MemberStatus.Approved;

    /// <summary>Why they asked to join, for the moderator reading the queue. L2-085.</summary>
    public string? ReasonForJoining { get; private set; }

    /// <summary>
    /// Begins a membership, awaiting a moderator.
    /// </summary>
    /// <remarks>
    /// The status is passed explicitly rather than left to the default. <see cref="MemberStatus"/>
    /// numbers <c>AwaitingApproval</c> zero, so a member constructed without saying would be
    /// approved only because the constructor's optional parameter says so - which is the kind of
    /// accident that lets somebody onto the board without being let on.
    /// </remarks>
    public static Member Join(
        Guid id,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood,
        string? reasonForJoining) =>
        new(id, congregationId, emailAddress, displayName, neighbourhood, MemberRole.Member, MemberStatus.AwaitingApproval)
        {
            ReasonForJoining = reasonForJoining,
        };

    /// <summary>A moderator lets them in.</summary>
    public void Approve()
    {
        if (Status != MemberStatus.AwaitingApproval)
        {
            throw new MemberNotAwaitingApprovalException(Id);
        }

        Status = MemberStatus.Approved;
    }

    /// <summary>A moderator does not.</summary>
    public void Decline()
    {
        if (Status != MemberStatus.AwaitingApproval)
        {
            throw new MemberNotAwaitingApprovalException(Id);
        }

        Status = MemberStatus.Declined;
    }

    /// <summary>
    /// Grants a role within this congregation.
    /// </summary>
    /// <remarks>
    /// Administrator is not grantable. An administrator is provisioned rather than promoted, and
    /// a moderator who could make themselves one would make the role gate decorative.
    /// </remarks>
    public void GrantRole(MemberRole role)
    {
        if (role == MemberRole.Administrator || Role == MemberRole.Administrator)
        {
            throw new RoleNotGrantableException(Id, role);
        }

        if (Status != MemberStatus.Approved)
        {
            throw new MemberNotApprovedException(Id);
        }

        Role = role;
    }

    public static string Normalise(string emailAddress) =>
        emailAddress.Trim().ToLowerInvariant();
}
