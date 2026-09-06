using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member fills in to ask to borrow something.
/// </summary>
/// <remarks>
/// The listing is a route value rather than a field, so the thing being asked for is part of the
/// address of the ask.
/// <para>
/// The refused fields are declared on the body model rather than on the command, because the
/// body model is what the endpoint binds and so is the only thing the inspector can read.
/// <c>L2-063</c>.
/// </para>
/// </remarks>
public sealed record MakeLoanRequestRequest(
    string Message,
    DateOnly? PickupOn,
    DateOnly? ReturnBy,
    bool LoanAcknowledged) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } = PaymentDeliveryAndDepositFields.And("price");
}
