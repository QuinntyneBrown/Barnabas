using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member may change about themselves.
/// </summary>
/// <remarks>
/// The email address is refused by name rather than absent. It is the only way back into the
/// product, and changing it is a different act from correcting a description — so an attempt is
/// reported rather than quietly discarded.
/// </remarks>
public sealed record EditMyProfileRequest(
    string DisplayName,
    string Neighbourhood,
    string? Description,
    IReadOnlyList<string>? HelpTags) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("emailAddress", "memberId", "role", "status");
}
