using Barnabas.Application.Common.Validation;
using Barnabas.Application.Listings.PostLendListing;
using MediatR;

namespace Barnabas.Application.Listings.PostHelpListing;

/// <summary>
/// Posts an offer of time.
/// </summary>
/// <remarks>
/// Help offers time rather than an object, so it carries no price and no photo, and it declares
/// the windows in which the offer actually holds. A requester chooses one of those windows, so
/// they are the substance of the listing rather than a note on it - <c>L2-030</c>, <c>L2-057</c>.
/// </remarks>
public sealed record PostHelpListingCommand(
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    IReadOnlyList<AvailabilityWindowInput>? Windows) : IRequest<PostedListingResult>, IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("price", "photo");
}
