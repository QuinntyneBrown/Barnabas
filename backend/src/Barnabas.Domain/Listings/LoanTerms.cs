namespace Barnabas.Domain.Listings;

/// <summary>
/// The terms a Lend listing carries: the period within which the owner expects the item back.
/// </summary>
/// <remarks>
/// An owned value type on <see cref="Listing"/>, not a separate table. A Lend listing has
/// exactly one of these and it has no life of its own.
/// </remarks>
public sealed record LoanTerms(DateOnly ReturnBy);
