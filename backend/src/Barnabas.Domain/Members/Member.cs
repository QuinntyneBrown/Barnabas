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

    public static string Normalise(string emailAddress) =>
        emailAddress.Trim().ToLowerInvariant();
}
