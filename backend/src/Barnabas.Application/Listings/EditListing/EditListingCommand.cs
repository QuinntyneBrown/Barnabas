using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Validation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Listings.EditListing;

/// <summary>
/// Corrects the details of a listing already on the board.
/// </summary>
/// <remarks>
/// The four fields every kind carries, and none of the per-kind terms. A loan's return date and
/// a sale's price are what the kinds differ by; editing them belongs with the form that collects
/// them, and no acceptance criterion asks for it yet.
/// <para>
/// <c>kind</c> is refused rather than ignored. Without the declaration a submitted kind would
/// find nothing to bind to, be discarded, and the edit would answer 200 - so L2-036 AC2, which
/// requires 400, could not be satisfied at all.
/// </para>
/// </remarks>
public sealed record EditListingCommand(
    Guid ListingId,
    string Title,
    string Description,
    string Category,
    string Neighbourhood) : IRequest<EditedListingResult>, IRequireOwnership<Listing>, IForbidFields
{
    public Guid ResourceId => ListingId;

    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("kind");
}
