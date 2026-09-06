namespace Barnabas.Domain.Access;

/// <summary>
/// Raised when a refresh token has expired, been revoked, or already been rotated.
/// </summary>
/// <remarks>Mapped to 401 Unauthorized.</remarks>
public sealed class RefreshTokenNotActiveException : Exception
{
    public RefreshTokenNotActiveException()
        : base("The refresh token is no longer active.")
    {
    }
}
