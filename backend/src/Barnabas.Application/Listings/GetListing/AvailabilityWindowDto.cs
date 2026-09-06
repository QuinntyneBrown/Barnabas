namespace Barnabas.Application.Listings.GetListing;

/// <summary>
/// One window a Help listing declares.
/// </summary>
/// <remarks>
/// The identifier is here because a request has to name the window it wants, and the screen that
/// offers the windows is the one that has to send it back.
/// </remarks>
public sealed record AvailabilityWindowDto(
    Guid AvailabilityWindowId,
    DayOfWeek Day,
    TimeOnly StartsAt,
    TimeOnly EndsAt);
