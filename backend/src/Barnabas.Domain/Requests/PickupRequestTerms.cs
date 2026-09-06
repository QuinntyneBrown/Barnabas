namespace Barnabas.Domain.Requests;

/// <summary>
/// The terms a Give or Sell request carries: when the requester proposes to collect the thing.
/// </summary>
/// <remarks>
/// Ownership transfers in both kinds, so there is no return date, and Barnabas arranges no
/// delivery - the two members meet. A Sell request carries no money either: the price is on the
/// listing and is settled between them in person.
/// </remarks>
public sealed record PickupRequestTerms(DateTimeOffset PickupAt);
