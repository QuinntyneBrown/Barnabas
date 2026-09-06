using Barnabas.Domain.Moderation;

namespace Barnabas.Application.Moderation.GetModerationQueue;

/// <summary>
/// One complaint, as a moderator sees it.
/// </summary>
/// <remarks>
/// This is the only projection anywhere that names a reporter, which is what makes
/// <c>L2-081</c> a property of the model rather than a rule somebody has to remember: no
/// member-facing type has a field to put one in.
/// </remarks>
public sealed record ReportedListingDto(
    Guid ReportId,
    ReportReason Reason,
    string? Note,
    Guid ReporterId,
    string ReporterDisplayName,
    DateTimeOffset ReportedAt);
