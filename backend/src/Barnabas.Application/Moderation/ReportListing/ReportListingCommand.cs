using Barnabas.Domain.Moderation;
using MediatR;

namespace Barnabas.Application.Moderation.ReportListing;

/// <summary>
/// Tells the moderators about a listing.
/// </summary>
/// <remarks>
/// The reporter is not a field here. It comes from the verified session, so there is nothing in
/// the body that could report a listing in somebody else's name.
/// </remarks>
public sealed record ReportListingCommand(Guid ListingId, ReportReason Reason, string? Note)
    : IRequest<ReportedListingResult>;
