using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using MediatR;

namespace Barnabas.Application.Access.ExchangeSignInToken;

/// <summary>
/// Consumes the link, opens a session, and issues the pair of tokens bound to it.
/// </summary>
/// <remarks>
/// Expiry is a question the entity can answer; single use is a question only the database can.
/// Consumption is therefore one conditional update whose affected-row count decides the winner,
/// not a check followed by a save - two callers can both read an unconsumed token, and only the
/// database can refuse the second write.
/// <para>
/// Both failures answer 410 rather than 404. The token was real, and saying so lets the screen
/// offer to send another link instead of implying the member mistyped something.
/// </para>
/// </remarks>
public sealed class ExchangeSignInTokenCommandHandler : IRequestHandler<ExchangeSignInTokenCommand, SessionResult>
{
    private readonly IAuthenticationStore _store;
    private readonly ISecretService _secrets;
    private readonly IAccessTokenIssuer _issuer;
    private readonly TimeProvider _time;

    public ExchangeSignInTokenCommandHandler(
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

    public async Task<SessionResult> Handle(ExchangeSignInTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _time.GetUtcNow();

        var consumed = await _store.TryConsumeSignInTokenAsync(_secrets.Hash(request.Token), now, cancellationToken)
            ?? throw new SignInTokenNotRedeemableException();

        // Expiry is the entity's own question, and it is asked after consumption rather than
        // before: a link that arrives too late is spent either way, and consuming it first means
        // an expired link cannot then be retried in the hope of better timing.
        if (consumed.HasExpired(now))
        {
            throw new SignInTokenNotRedeemableException();
        }

        var member = await _store.FindMemberByIdAsync(consumed.MemberId, cancellationToken)
            ?? throw new SignInTokenNotRedeemableException();

        // The congregation comes from the member record, never from anything the caller sent.
        var session = new Session(Guid.NewGuid(), member.CongregationId, member.Id, now);

        await _store.AddSessionAsync(session, cancellationToken);

        var refreshSecret = _secrets.CreateSecret();

        await _store.AddRefreshTokenAsync(
            new RefreshToken(Guid.NewGuid(), member.CongregationId, session.Id, _secrets.Hash(refreshSecret), now),
            cancellationToken);

        var access = _issuer.Issue(member, session);

        return new SessionResult(session.Id, access.Value, access.ExpiresOn, refreshSecret);
    }
}
