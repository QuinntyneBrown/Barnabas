namespace Barnabas.Application.Listings.PostHelpListing;

/// <summary>
/// One window as a member submits it, before it becomes a declared window on a listing.
/// </summary>
/// <remarks>
/// A submitted window carries no identifier: the identifiers belong to the listing and are
/// minted when it is created, so nothing a caller sends can choose one. That is what keeps a
/// request from naming a window that was never declared.
/// <para>
/// Every member is nullable so that a window submitted with a field missing binds and is refused
/// by the validator naming it, rather than failing during deserialization.
/// </para>
/// </remarks>
public sealed record AvailabilityWindowInput(DayOfWeek? Day, TimeOnly? StartsAt, TimeOnly? EndsAt);
