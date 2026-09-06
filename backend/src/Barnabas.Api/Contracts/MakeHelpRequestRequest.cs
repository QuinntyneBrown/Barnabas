using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member fills in to ask for help.
/// </summary>
/// <remarks>
/// A message and the window they want, chosen from the ones the offer declared. There is no
/// pickup, because help is time rather than a thing to collect - <c>L2-057</c>.
/// </remarks>
public sealed record MakeHelpRequestRequest(
    string Message,
    Guid? AvailabilityWindowId) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("price", "returnBy");
}
