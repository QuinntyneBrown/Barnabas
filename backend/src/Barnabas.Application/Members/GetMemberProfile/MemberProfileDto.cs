using Barnabas.Domain.Listings;

namespace Barnabas.Application.Members.GetMemberProfile;

/// <summary>One of the member's listings, as their profile lists it.</summary>
public sealed record ProfileListingDto(Guid ListingId, ListingKind Kind, string Title, decimal? Price);

/// <summary>
/// Another member, as the congregation sees them.
/// </summary>
/// <remarks>
/// There is no email address on this type, and that is the point rather than an omission:
/// <c>L2-024 AC2</c> forbids one in any response describing another member, and a field that does
/// not exist cannot be added by accident.
/// <para>
/// No action to message them either. A request against a listing, once accepted, is the only
/// thing that opens a thread - <c>L2-069</c>.
/// </para>
/// </remarks>
public sealed record MemberProfileDto(
    Guid MemberId,
    string DisplayName,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string> HelpTags,
    IReadOnlyList<ProfileListingDto> ActiveListings);
