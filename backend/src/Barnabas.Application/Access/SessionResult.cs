namespace Barnabas.Application.Access;

/// <summary>
/// Everything a successful sign-in or renewal produces.
/// </summary>
/// <remarks>
/// The refresh token is here because the controller has to put it somewhere, and nowhere else
/// in the application layer knows it exists. It does not reach the response body: the API
/// writes it into an HttpOnly cookie, which is the only place a long-lived secret belongs once
/// a browser is holding it.
/// </remarks>
public sealed record SessionResult(
    Guid SessionId,
    string AccessToken,
    DateTimeOffset ExpiresOn,
    string RefreshToken);
