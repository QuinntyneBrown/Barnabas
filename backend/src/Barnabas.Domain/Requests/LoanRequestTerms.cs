namespace Barnabas.Domain.Requests;

/// <summary>
/// The terms a Lend request carries: when the requester would collect the item and when
/// they would bring it back.
/// </summary>
/// <remarks>
/// The acknowledgement that the item remains the owner's is a condition of submitting,
/// not a term of the request, so it is enforced by the validator and not recorded here.
/// There is nothing to show the owner: a request that reached them was acknowledged.
/// </remarks>
public sealed record LoanRequestTerms(DateOnly PickupOn, DateOnly ReturnBy);
