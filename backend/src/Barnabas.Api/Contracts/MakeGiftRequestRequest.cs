using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member fills in to ask for something being given away.
/// </summary>
/// <remarks>
/// A message and when they could collect it. No return date, because ownership transfers, and
/// no price, because a gift has none - <c>L2-055</c>.
/// </remarks>
public sealed record MakeGiftRequestRequest(
    string Message,
    DateTimeOffset? PickupAt) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("price", "returnBy");
}
