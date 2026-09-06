using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What an administrator may change about a congregation.
/// </summary>
/// <remarks>
/// The slug is refused by name rather than absent, so an attempt to change it is reported instead
/// of being discarded — <c>L2-001 AC3</c>.
/// </remarks>
public sealed record ConfigureCongregationRequest(
    string Name,
    IReadOnlyList<string>? Neighbourhoods) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("slug");
}
