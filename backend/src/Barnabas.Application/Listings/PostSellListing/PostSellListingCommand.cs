using Barnabas.Application.Common.Validation;
using Barnabas.Application.Listings.PostLendListing;
using MediatR;

namespace Barnabas.Application.Listings.PostSellListing;

/// <summary>
/// Posts something the owner is selling.
/// </summary>
/// <remarks>
/// The price is nullable so that a form submitted without one binds and is then refused by the
/// validator naming the field, for the same reason a Lend listing's return date is nullable:
/// left non-nullable it would fail during deserialization and the member would get the
/// serializer's complaint instead of a sentence they can act on.
/// <para>
/// The price is a stated asking figure and nothing more. Barnabas takes no payment, so the
/// payment-instrument names are refused here as everywhere - <c>L2-034</c>.
/// </para>
/// </remarks>
public sealed record PostSellListingCommand(
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    string Condition,
    decimal? Price) : IRequest<PostedListingResult>, IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } = PaymentDeliveryAndDepositFields.Names;
}
