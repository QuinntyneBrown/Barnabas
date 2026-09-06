using Barnabas.Domain.Requests;

namespace Barnabas.Application.Requests.Common;

/// <summary>
/// The terms of a request, as a line a screen can render without knowing the kind.
/// </summary>
/// <remarks>
/// Flattened to text on purpose. Both inbox screens read requests, and a reader there is
/// scanning rather than editing - so projecting the per-kind value types would make the screen
/// branch over four shapes to render four sentences.
/// </remarks>
public static class LoanTermsText
{
    public static string For(LoanRequestTerms? terms) => terms is null
        ? string.Empty
        : $"Pickup {terms.PickupOn:yyyy-MM-dd}, back by {terms.ReturnBy:yyyy-MM-dd}";
}
