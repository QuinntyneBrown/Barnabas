namespace Barnabas.Domain.Listings;

/// <summary>
/// Which of the four a listing is, chosen before any detail is entered and fixed thereafter.
/// </summary>
/// <remarks>
/// The four are not interchangeable. Lend keeps ownership and needs a date the item comes
/// back. Give transfers ownership and has no price. Sell states an asking figure Barnabas
/// never handles. Help offers time rather than an object.
/// <para>
/// Feature slice 1 posts only <see cref="Lend"/>. All four are declared here from the start
/// because <see cref="Listing.CloseOut"/> maps every kind to its own outcome, and L2-037
/// tests all four of those outcomes.
/// </para>
/// </remarks>
public enum ListingKind
{
    Lend = 0,
    Give = 1,
    Sell = 2,
    Help = 3,
}
