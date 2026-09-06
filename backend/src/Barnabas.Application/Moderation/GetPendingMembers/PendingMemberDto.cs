namespace Barnabas.Application.Moderation.GetPendingMembers;

/// <summary>
/// One applicant, as the moderator deciding about them sees it.
/// </summary>
/// <remarks>
/// The reason they gave is here and nowhere else. It is written for the moderators rather than
/// for the congregation, so the directory and the public profile do not carry it.
/// </remarks>
public sealed record PendingMemberDto(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    string? ReasonForJoining);
