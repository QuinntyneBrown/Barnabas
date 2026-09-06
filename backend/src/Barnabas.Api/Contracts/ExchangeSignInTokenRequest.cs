namespace Barnabas.Api.Contracts;

/// <summary>The secret carried by a sign-in link.</summary>
public sealed record ExchangeSignInTokenRequest(string Token);
