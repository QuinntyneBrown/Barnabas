namespace Barnabas.Domain.Congregations;

/// <summary>Raised when the entitlement to finish joining has expired or already been spent.</summary>
public sealed class JoiningSessionNotUsableException : Exception
{
    public JoiningSessionNotUsableException()
        : base("That joining session is no longer open. Redeem your code again.")
    {
    }
}
