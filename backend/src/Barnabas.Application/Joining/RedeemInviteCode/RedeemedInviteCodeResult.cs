namespace Barnabas.Application.Joining.RedeemInviteCode;

/// <summary>
/// What somebody needs to finish joining: which congregation, what to call a neighbourhood, and
/// the entitlement to come back with a profile.
/// </summary>
/// <remarks>
/// The joining token rather than the congregation's identifier. Handing back an identifier would
/// let the next call name any congregation it liked; a bearer token names only the one this code
/// belonged to.
/// </remarks>
public sealed record RedeemedInviteCodeResult(
    string CongregationName,
    IReadOnlyList<string> Neighbourhoods,
    string JoiningToken,
    DateTimeOffset ExpiresAt);
