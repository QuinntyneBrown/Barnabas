using MediatR;

namespace Barnabas.Application.Access.RequestSignInLink;

/// <summary>Asks for a single-use sign-in link to be sent to a member's address.</summary>
public sealed record RequestSignInLinkCommand(string EmailAddress) : IRequest;
