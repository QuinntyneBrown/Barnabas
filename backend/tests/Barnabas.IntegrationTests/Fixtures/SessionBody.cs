namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// The body a successful sign-in returns.
/// </summary>
/// <remarks>
/// There is deliberately no refresh token here. It travels as an HttpOnly cookie, out of reach
/// of any script on the page, and a test that could read it from the body would be asserting
/// the wrong contract.
/// </remarks>
public sealed record SessionBody(Guid SessionId, string AccessToken, DateTimeOffset ExpiresOn);
