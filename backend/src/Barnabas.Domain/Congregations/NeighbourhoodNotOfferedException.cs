namespace Barnabas.Domain.Congregations;

/// <summary>
/// Raised when a neighbourhood is named that the congregation does not offer.
/// </summary>
/// <remarks>
/// Each congregation carries its own ordered set, and free text is not accepted — a parish's
/// neighbourhoods are what its members recognise, not what a form will take. <c>L2-002 AC2</c>.
/// </remarks>
public sealed class NeighbourhoodNotOfferedException : Exception
{
    public NeighbourhoodNotOfferedException(string neighbourhood)
        : base("That is not one of your congregation's neighbourhoods.") => Neighbourhood = neighbourhood;

    public string Neighbourhood { get; }
}
