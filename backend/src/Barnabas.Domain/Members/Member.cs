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
    public const int DescriptionMaxLength = 1000;

    /// <summary>
    /// How many kinds of help one member may declare.
    /// </summary>
    /// <remarks>
    /// Ten, so a profile says something. A member who has ticked everything has told the
    /// congregation nothing, and the directory's tag search would return everybody.
    /// </remarks>
    public const int MaxHelpTags = 10;

    private readonly List<MemberHelpTag> _helpTags = [];

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

    /// <summary>A few words in the member's own voice, shown on their public profile.</summary>
    public string? Description { get; private set; }

    /// <summary>The kinds of help they have said they can offer.</summary>
    public IReadOnlyList<MemberHelpTag> HelpTags => _helpTags;

    /// <summary>When they left. A departed member is on no board and in no directory.</summary>
    public DateTimeOffset? LeftAt { get; private set; }

    /// <summary>When their personal data was erased at their request. L2-101 AC2.</summary>
    public DateTimeOffset? ErasedAt { get; private set; }

    public bool IsErased => ErasedAt is not null;

    /// <summary>What a member's name reads as once they have been erased.</summary>
    public const string ErasedDisplayName = "A former member";

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

    /// <summary>
    /// Changes what the congregation sees of them.
    /// </summary>
    /// <remarks>
    /// The neighbourhood's membership of the congregation's set and the tags' membership of its
    /// help-tag set are not checked here. Both are relationships between two aggregates, and this
    /// one holds only itself - the handler that loaded the congregation settles them.
    /// </remarks>
    public void UpdateProfile(
        string displayName,
        string neighbourhood,
        string? description,
        IReadOnlyCollection<string> helpTags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(helpTags);

        if (helpTags.Count > MaxHelpTags)
        {
            throw new ArgumentException($"Declare at most {MaxHelpTags} kinds of help.", nameof(helpTags));
        }

        DisplayName = displayName;
        Neighbourhood = neighbourhood;
        Description = description;

        _helpTags.Clear();
        _helpTags.AddRange(helpTags.Select(tag => new MemberHelpTag(Id, tag)));
    }

    /// <summary>
    /// They leave.
    /// </summary>
    /// <remarks>
    /// Their listings are withdrawn by the handler rather than here: a member holds no listings,
    /// and reaching into them from this aggregate would be the coupling the boundary exists to
    /// prevent. What this records is that they have gone.
    /// </remarks>
    public void Leave(DateTimeOffset asOf)
    {
        Status = MemberStatus.Left;
        LeftAt = asOf;
    }

    /// <summary>
    /// Erases them: the name goes, the address goes, and everything they wrote about themselves
    /// goes.
    /// </summary>
    /// <remarks>
    /// Anonymised in place rather than deleted, and irreversibly. The row is what every listing,
    /// request and thread they were part of points at, and deleting it would break the other
    /// party's record of a conversation they are entitled to keep - <c>L2-066 AC1</c>. What is
    /// left is a tombstone: a member existed, and nothing about who.
    /// <para>
    /// The address is replaced with one derived from the identifier under <c>.invalid</c>, which
    /// is reserved by RFC 2606 and can never be delivered to. It is not the old address hashed:
    /// a hash of a known address is a lookup table away from being the address again, and
    /// <c>L2-101</c> asks for irreversible.
    /// </para>
    /// <para>
    /// Leaving is a separate act and remains one. Somebody may leave and stay reachable; erasure
    /// is what they ask for when they want to be forgotten, and it implies leaving.
    /// </para>
    /// </remarks>
    public void Erase(DateTimeOffset asOf)
    {
        if (IsErased)
        {
            return;
        }

        DisplayName = ErasedDisplayName;
        EmailAddress = $"erased-{Id:n}@erased.invalid";
        Neighbourhood = string.Empty;
        Description = null;
        ReasonForJoining = null;
        Status = MemberStatus.Left;
        LeftAt ??= asOf;
        ErasedAt = asOf;

        _helpTags.Clear();
    }

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
