using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member fills in to ask to buy something.
/// </summary>
/// <remarks>
/// A message and when they could collect it, and nothing about money. The price is on the
/// listing and is settled between the two members in person; Barnabas neither takes it nor
/// records an offer against it - <c>L2-056</c>, <c>L2-063</c>.
/// </remarks>
public sealed record MakePurchaseRequestRequest(
    string Message,
    DateTimeOffset? PickupAt) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("price", "offer", "returnBy");
}
