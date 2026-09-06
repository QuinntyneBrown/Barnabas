namespace Barnabas.Application.Moderation.ReportListing;

/// <summary>
/// What the reporter is told back.
/// </summary>
/// <remarks>
/// It names the report and nothing about the outcome. A member who reported a listing learns
/// that a moderator will look, not what a moderator decided - <c>L2-081</c> runs in both
/// directions, and the owner is the one entitled to know their listing was removed.
/// </remarks>
public sealed record ReportedListingResult(Guid ReportId, Guid ListingId);
