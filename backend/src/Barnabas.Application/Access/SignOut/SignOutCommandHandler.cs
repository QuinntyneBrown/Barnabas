using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;

namespace Barnabas.Application.Access.SignOut;

/// <summary>
/// Revokes the session the caller's token names, and the refresh token bound to it.
/// </summary>
/// <remarks>
/// The session is revoked rather than the token. That is what makes a signed-out access token
/// stop working with no window, and it is also what leaves a member's other device signed in:
/// each sign-in holds its own session, so exactly one is ended.
/// </remarks>
public sealed class SignOutCommandHandler : IRequestHandler<SignOutCommand>
{
    private readonly IAuthenticationStore _store;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public SignOutCommandHandler(
        IAuthenticationStore store,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _store = store;
        _congregation = congregation;
        _time = time;
    }

    public Task Handle(SignOutCommand request, CancellationToken cancellationToken) =>
        _store.RevokeSessionAsync(_congregation.SessionId, _time.GetUtcNow(), cancellationToken);
}
