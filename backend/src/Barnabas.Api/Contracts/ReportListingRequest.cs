using Barnabas.Application.Common.Validation;
using Barnabas.Domain.Moderation;

namespace Barnabas.Api.Contracts;

/// <summary>
/// What a member says when they report a listing.
/// </summary>
/// <remarks>
/// The listing is a route value and the reporter comes from the session, so neither is a field
/// here. A free-text note is exactly where somebody would paste the card number they were
/// complaining about, which is why the usual vocabulary is refused by name.
/// </remarks>
public sealed record ReportListingRequest(ReportReason Reason, string? Note) : IForbidFields
{
    public static IReadOnlySet<string> ForbiddenFields { get; } = PaymentDeliveryAndDepositFields.Names;
}
