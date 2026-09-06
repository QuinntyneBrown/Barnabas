using Barnabas.Application.Common.Abuse;
using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Email;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using MediatR;

namespace Barnabas.Application.Access.RequestSignInLink;

/// <summary>
/// Finds the member, issues a token, stores its hash, and hands the secret to the mail.
/// </summary>
/// <remarks>
/// The lookup goes through <see cref="IAuthenticationStore"/> rather than the filtered context,
/// because signing in is anonymous: only the resulting record says which congregation the
/// member belongs to, so there is nothing to filter by yet.
/// <para>
/// When no member matches, the handler does the same work and returns the same answer. That
/// costs a hash nobody needs, and it is what keeps the endpoint from being turned into a way to
/// discover who is in the congregation.
/// </para>
/// </remarks>
public sealed class RequestSignInLinkCommandHandler : IRequestHandler<RequestSignInLinkCommand>
{
    private readonly IAuthenticationStore _store;
    private readonly ISecretService _secrets;
    private readonly IEmailSender _email;
    private readonly ISignInLinkThrottle _throttle;
    private readonly TimeProvider _time;

    public RequestSignInLinkCommandHandler(
        IAuthenticationStore store,
        ISecretService secrets,
        IEmailSender email,
        ISignInLinkThrottle throttle,
        TimeProvider time)
    {
        _store = store;
        _secrets = secrets;
        _email = email;
        _throttle = throttle;
        _time = time;
    }

    public async Task Handle(RequestSignInLinkCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Counted before the address is looked up, so an unregistered address is throttled exactly
        // as a registered one is. Throttling only the addresses that exist would turn the limiter
        // itself into the disclosure L2-013 forbids.
        if (!_throttle.TryRecord(request.EmailAddress))
        {
            throw new TooManyRequestsException();
        }

        var member = await _store.FindSignableMemberByEmailAsync(request.EmailAddress, cancellationToken);

        var secret = _secrets.CreateSecret();
        var hash = _secrets.Hash(secret);

        if (member is null)
        {
            return;
        }

        var token = new SignInToken(Guid.NewGuid(), member.CongregationId, member.Id, hash, _time.GetUtcNow());

        await _store.AddSignInTokenAsync(token, cancellationToken);

        await _email.SendSignInLinkAsync(member.EmailAddress, secret, cancellationToken);
    }
}
