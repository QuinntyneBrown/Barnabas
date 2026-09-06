using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using MediatR;

namespace Barnabas.Application.Access.RefreshSession;

/// <summary>
/// Rotates the refresh token and issues a fresh access token for the same session.
/// </summary>
/// <remarks>
/// A refresh token is replaced every time it is exchanged, so presenting an old one is evidence
/// of a copy and is refused. The rotation is a conditional update on the row still being
/// unrotated, so two callers racing produce one winner and one 401 rather than two valid tokens.
/// </remarks>
public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, SessionResult>
{
    private readonly IAuthenticationStore _store;
    private readonly ISecretService _secrets;
    private readonly IAccessTokenIssuer _issuer;
    private readonly TimeProvider _time;

    public RefreshSessionCommandHandler(
        IAuthenticationStore store,
        ISecretService secrets,
        IAccessTokenIssuer issuer,
        TimeProvider time)
    {
        _store = store;
        _secrets = secrets;
        _issuer = issuer;
        _time = time;
    }

    public async Task<SessionResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _time.GetUtcNow();

        var presented = await _store.FindRefreshTokenAsync(_secrets.Hash(request.RefreshToken), cancellationToken)
            ?? throw new RefreshTokenNotActiveException();

        // Revoked, already rotated, or simply too old. The race is settled below; this is the
        // ordinary case, and it is the entity that knows the rules.
        if (!presented.IsActive(now))
        {
            throw new RefreshTokenNotActiveException();
        }

        var session = await _store.FindSessionAsync(presented.SessionId, cancellationToken);

        // Signing out revokes the session, so a token bound to a dead session is dead with it.
        if (session is null || !session.IsLive(now))
        {
            throw new RefreshTokenNotActiveException();
        }

        var replacementId = Guid.NewGuid();

        if (!await _store.TryRotateRefreshTokenAsync(presented.Id, replacementId, now, cancellationToken))
        {
            throw new RefreshTokenNotActiveException();
        }

        var member = await _store.FindMemberByIdAsync(session.MemberId, cancellationToken)
            ?? throw new RefreshTokenNotActiveException();

        var refreshSecret = _secrets.CreateSecret();

        await _store.AddRefreshTokenAsync(
            new RefreshToken(replacementId, session.CongregationId, session.Id, _secrets.Hash(refreshSecret), now),
            cancellationToken);

        var access = _issuer.Issue(member, session);

        return new SessionResult(session.Id, access.Value, access.ExpiresOn, refreshSecret, member.Status, member.Role);
    }
}
