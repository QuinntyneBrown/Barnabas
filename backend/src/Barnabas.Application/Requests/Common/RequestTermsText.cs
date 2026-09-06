using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;

namespace Barnabas.Application.Requests.Common;

/// <summary>
/// The terms of a request, as a line a screen can render without knowing the kind.
/// </summary>
/// <remarks>
/// Flattened to text on purpose. Both inbox screens read requests, and a reader there is
/// scanning rather than editing - so projecting the per-kind value types would make the screen
/// branch over four shapes to render four sentences.
/// <para>
/// The branch lives here instead, once, and it is a branch over which terms are present rather
/// than over the kind: a request carries exactly one set of terms, and which set it is says
/// what kind it is.
/// </para>
/// </remarks>
public static class RequestTermsText
{
    public static string For(ListingRequest request, Listing listing)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(listing);

        if (request.LoanTerms is { } loan)
        {
            return $"Pickup {loan.PickupOn:yyyy-MM-dd}, back by {loan.ReturnBy:yyyy-MM-dd}";
        }

        if (request.PickupTerms is { } pickup)
        {
            return $"Pickup {pickup.PickupAt:yyyy-MM-dd HH:mm}";
        }

        if (request.HelpTerms is { } help)
        {
            return WindowText(listing, help.AvailabilityWindowId);
        }

        return string.Empty;
    }

    /// <summary>
    /// The chosen window, in the words the offer used.
    /// </summary>
    /// <remarks>
    /// The window is read from the listing rather than copied onto the request, so an offer that
    /// is later edited does not leave the two disagreeing about what was asked for.
    /// </remarks>
    private static string WindowText(Listing listing, Guid availabilityWindowId)
    {
        var window = listing.AvailabilityWindows
            .FirstOrDefault(window => window.Id == availabilityWindowId);

        return window is null
            ? "A window the offer no longer declares"
            : $"{window.Day}, {window.StartsAt:HH':'mm}-{window.EndsAt:HH':'mm}";
    }
}
