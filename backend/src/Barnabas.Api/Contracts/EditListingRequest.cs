using Barnabas.Application.Common.Validation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member may change about a listing already on the board.
/// </summary>
/// <remarks>
/// The listing is a route value rather than a field, and the kind is neither. It is declared as
/// refused so a submitted one is named in a 400 rather than discarded - which is what L2-036 AC2
/// asks for, and what a body model with simply no kind property could not deliver.
/// </remarks>
public sealed record EditListingRequest(
    string Title,
    string Description,
    string Category,
    string Neighbourhood) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("kind");
}
