using MediatR;

namespace Barnabas.Application.Access.ExchangeSignInToken;

/// <summary>Exchanges the secret from a sign-in link for a session.</summary>
public sealed record ExchangeSignInTokenCommand(string Token) : IRequest<SessionResult>;
