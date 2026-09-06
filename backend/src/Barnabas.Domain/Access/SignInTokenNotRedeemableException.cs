namespace Barnabas.Domain.Access;

/// <summary>
/// Raised when a sign-in token has expired or has already been exchanged.
/// </summary>
/// <remarks>
/// Mapped to 410 Gone rather than 404. The token was real, and saying so lets the screen
/// offer to send another rather than implying the member mistyped something.
/// </remarks>
public sealed class SignInTokenNotRedeemableException : Exception
{
    public SignInTokenNotRedeemableException()
        : base("The sign-in link has expired or has already been used.")
    {
    }
}
