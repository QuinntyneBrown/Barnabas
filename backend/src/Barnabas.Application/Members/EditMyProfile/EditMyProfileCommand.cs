using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Validation;
using Barnabas.Application.Members.GetMyProfile;
using MediatR;

namespace Barnabas.Application.Members.EditMyProfile;

/// <summary>
/// Changes what the congregation sees of the caller.
/// </summary>
/// <remarks>
/// It names no member, so there is no identifier here that could edit somebody else's profile.
/// The email address is not editable: it is the only way back in, and changing it is a different
/// act from correcting a description.
/// </remarks>
public sealed record EditMyProfileCommand(
    string DisplayName,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string>? HelpTags) : IRequest<MyProfileDto>, IAllowUnapproved, IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("emailAddress", "memberId", "role", "status");
}
