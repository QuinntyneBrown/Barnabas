namespace Barnabas.Api.Contracts;

/// <summary>The address a sign-in link is asked for.</summary>
public sealed record RequestSignInLinkRequest(string EmailAddress);
