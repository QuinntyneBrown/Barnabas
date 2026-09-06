using MediatR;

namespace Barnabas.Application.Access.SignOut;

/// <summary>
/// Ends the caller's session.
/// </summary>
/// <remarks>
/// It carries no payload on purpose. The session is taken from the token's own claim, so a
/// member cannot sign anybody else out by naming them.
/// </remarks>
public sealed record SignOutCommand : IRequest;
