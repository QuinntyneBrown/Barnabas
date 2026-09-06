using Barnabas.Application.Common.Validation;
using MediatR;

namespace Barnabas.Application.Listings.PostLendListing;

/// <summary>
/// Posts something the owner wants back.
/// </summary>
/// <remarks>
/// There is no price property, and no owner or congregation either: the first because a loan is
/// not a sale, and the other two because they are taken from the session rather than the payload.
/// <para>
/// The forbidden field is what makes the absence of a price enforceable. Without it a submitted
/// price would simply find nothing to bind to, be discarded, and the listing would be created -
/// so L2-027, which requires the listing to be rejected, could not be satisfied at all.
/// </para>
/// </remarks>
public sealed record PostLendListingCommand(
    string Title,
    string Description,
    string Category,
    string Neighbourhood,
    DateOnly ReturnBy) : IRequest<PostedListingResult>, IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "price" };
}
