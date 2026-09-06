namespace Barnabas.Domain.Listings;

/// <summary>
/// Where a listing stands. Only <see cref="Active"/> appears on the board.
/// </summary>
/// <remarks>
/// The four closed-out statuses are not one neutral status, because the wording is the
/// distinction the whole product rests on: a member looking at last spring's listings
/// should be able to see which things were sold and which were given.
/// </remarks>
public enum ListingStatus
{
    Active = 0,

    /// <summary>A Lend listing that has come back, or one taken off the board.</summary>
    Archived = 1,

    /// <summary>A Sell listing that has been sold.</summary>
    Sold = 2,

    /// <summary>A Give listing that has been given away.</summary>
    GivenAway = 3,

    /// <summary>A Help offer that has been completed.</summary>
    Completed = 4,
}
