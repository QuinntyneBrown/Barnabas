namespace Barnabas.Application.Members.GetDirectory;

/// <summary>
/// One row of the congregation directory.
/// </summary>
/// <remarks>
/// No email address, by the shape of the type rather than by remembering to leave one out —
/// <c>L2-079 AC3</c>.
/// </remarks>
public sealed record DirectoryMemberDto(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    IReadOnlyList<string> HelpTags);
