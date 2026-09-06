namespace Barnabas.Api.Contracts;

/// <summary>
/// What a successful sign-in or renewal returns.
/// </summary>
/// <remarks>
/// The refresh token is deliberately absent. It travels as an HttpOnly cookie so that script on
/// the page cannot read it; putting it here as well would undo that in one line.
/// </remarks>
public sealed record SessionResponse(Guid SessionId, string AccessToken, DateTimeOffset ExpiresOn);
