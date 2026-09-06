using Barnabas.Application.Common.Validation;
using Barnabas.Application.Listings.PostLendListing;
using MediatR;

namespace Barnabas.Application.Listings.PostGiveListing;

/// <summary>
/// Posts something the owner is giving away.
/// </summary>
/// <remarks>
/// Ownership transfers and the members arrange the pickup between themselves, so a Give listing
/// carries no return date and no price. There is nothing here that a Lend listing does not also
/// have, and that is the point: what distinguishes a gift is what it lacks.
/// <para>
/// "Marked as free with pickup arranged by the members" is not stored. It follows from the kind,
/// and a column repeating what <see cref="Domain.Listings.ListingKind.Give"/> already says could
/// only ever disagree with it.
/// </para>
/// </remarks>
public sealed record PostGiveListingCommand(
    string Title,
    string Description,
    string Category,
    string Neighbourhood) : IRequest<PostedListingResult>, IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("price");
}
