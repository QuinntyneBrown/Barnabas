namespace Barnabas.Application.Members.ExportMyData;

/// <summary>The member's own record, including the address no other member's view carries.</summary>
public sealed record ExportedProfile(
    Guid MemberId,
    string DisplayName,
    string EmailAddress,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    string Role,
    string Status,
    string? ReasonForJoining,
    string CongregationName);
