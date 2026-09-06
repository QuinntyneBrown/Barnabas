namespace Barnabas.Domain.Congregations;

/// <summary>
/// Raised when a code that exists cannot be used.
/// </summary>
/// <remarks>
/// Expired, already redeemed, and revoked are one answer deliberately. Telling somebody which of
/// the three it was would say something about a code they are not entitled to know about, and
/// <c>L2-007 AC3</c> asks for exactly this indifference.
/// </remarks>
public sealed class InviteCodeNotRedeemableException : Exception
{
    public InviteCodeNotRedeemableException()
        : base("That code cannot be used. Ask whoever invited you for a fresh one.")
    {
    }
}
