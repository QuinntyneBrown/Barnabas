namespace Barnabas.Application.Members.GetMyProfile;

/// <summary>
/// A member's own profile, as their settings screen shows it.
/// </summary>
/// <remarks>
/// Carries the caller's own email address, unlike every other member projection. <c>L2-024 AC2</c>
/// forbids an address in a response describing <em>another</em> member; a member reading their own
/// settings is entitled to see where their sign-in link goes.
/// <para>
/// The congregation's neighbourhoods and help tags travel with it so the form has something to
/// offer without a second call, and so what it offers is this congregation's and no other's.
/// </para>
/// </remarks>
public sealed record MyProfileDto(
    Guid MemberId,
    string DisplayName,
    string EmailAddress,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    IReadOnlyList<string> Neighbourhoods,
    IReadOnlyList<string> AvailableHelpTags);
