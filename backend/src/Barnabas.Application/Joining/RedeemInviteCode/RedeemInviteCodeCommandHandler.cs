using Barnabas.Application.Common.Abuse;
using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Application.Joining.Common;
using Barnabas.Domain.Congregations;
using MediatR;
using Microsoft.Extensions.Options;

namespace Barnabas.Application.Joining.RedeemInviteCode;

/// <summary>
/// Spends the code, and issues the entitlement to finish joining.
/// </summary>
/// <remarks>
/// Two answers and one silence. A code nobody has ever issued is not found; a code that exists but
/// cannot be used says only that — expired, spent and revoked are one reply, because telling
/// somebody which would say something about a code they are not entitled to know about.
/// <para>
/// The order matters. The conditional update runs first and settles who wins the race; the read
/// that follows answers expiry and revocation, which are not races. A caller who loses the race
/// gets the same reply as one presenting a code that was used yesterday, and that is correct:
/// from where they stand the code is spent either way.
/// </para>
/// </remarks>
public sealed class RedeemInviteCodeCommandHandler
    : IRequestHandler<RedeemInviteCodeCommand, RedeemedInviteCodeResult>
{
    private readonly IInvitationStore _invitations;
    private readonly IRedemptionThrottle _throttle;
    private readonly ICallerSource _source;
    private readonly ISecretService _secrets;
    private readonly TimeProvider _time;
    private readonly InviteCodeOptions _options;

    /// <summary>How long a source that has been guessing is asked to wait.</summary>
    private static readonly TimeSpan RedemptionRetryAfter = TimeSpan.FromHours(1);

    public RedeemInviteCodeCommandHandler(
        IInvitationStore invitations,
        IRedemptionThrottle throttle,
        ICallerSource source,
        ISecretService secrets,
        TimeProvider time,
        IOptions<InviteCodeOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _invitations = invitations;
        _throttle = throttle;
        _source = source;
        _secrets = secrets;
        _time = time;
        _options = options.Value;
    }

    public async Task<RedeemedInviteCodeResult> Handle(
        RedeemInviteCodeCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = InviteCode.Normalise(request.Code);
        var now = _time.GetUtcNow();

        // Guessing is what this stops. Somebody redeeming their own code correctly never meets it;
        // somebody working through the alphabet meets it quickly. L2-099 AC2.
        if (!_throttle.MayTry(_source.Key))
        {
            throw new TooManyRequestsException(RedemptionRetryAfter);
        }

        var redeemed = await _invitations.TryRedeemAsync(code, now, cancellationToken);

        if (redeemed is null)
        {
            // Either it was never a code, or somebody else has already spent it. The two are
            // different answers and the second read is what tells them apart.
            var existing = await _invitations.FindCodeAsync(code, cancellationToken);

            _throttle.RecordFailure(_source.Key);

            throw existing is null
                ? new NotFoundException()
                : new InviteCodeNotRedeemableException();
        }

        // Won the race, but the code may still have been dead when it was spent. Expiry and
        // revocation are properties of the row rather than of the contention.
        if (redeemed.RevokedAt is not null || now >= redeemed.ExpiresAt)
        {
            _throttle.RecordFailure(_source.Key);

            throw new InviteCodeNotRedeemableException();
        }

        var congregation = await _invitations.FindCongregationAsync(redeemed.CongregationId, cancellationToken)
            ?? throw new NotFoundException();

        var token = _secrets.CreateSecret();
        var expiresAt = now.Add(_options.JoiningSessionLifetime);

        await _invitations.AddJoiningSessionAsync(
            new JoiningSession(
                Guid.NewGuid(),
                congregation.Id,
                redeemed.Id,
                _secrets.Hash(token),
                now,
                expiresAt),
            cancellationToken);

        return new RedeemedInviteCodeResult(
            congregation.Name,
            congregation.Neighbourhoods,
            token,
            expiresAt);
    }
}
