using System.Collections.Concurrent;
using Barnabas.Application.Common.Email;
using Barnabas.Domain.Members;
using Microsoft.Extensions.Options;

namespace Barnabas.Infrastructure.Email;

/// <summary>
/// Records sign-in links instead of sending them, for development and for the acceptance suites.
/// </summary>
/// <remarks>
/// Registered as a singleton so that what one request dispatched, the next request can read.
/// A production deployment replaces this with a real transport; nothing else changes, because
/// the handler depends on <see cref="IEmailSender"/> and not on this.
/// </remarks>
public sealed class InMemoryEmailSender : IEmailSender, IEmailOutbox
{
    private readonly ConcurrentQueue<SignInLinkMessage> _sent = new();
    private readonly SignInLinkOptions _options;
    private readonly TimeProvider _time;

    public InMemoryEmailSender(IOptions<SignInLinkOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public Task SendSignInLinkAsync(string emailAddress, string token, CancellationToken cancellationToken)
    {
        var url = _options.UrlTemplate.Replace("{token}", Uri.EscapeDataString(token), StringComparison.Ordinal);

        _sent.Enqueue(new SignInLinkMessage(Member.Normalise(emailAddress), token, url, _time.GetUtcNow()));

        return Task.CompletedTask;
    }

    public SignInLinkMessage? LatestFor(string emailAddress)
    {
        var normalised = Member.Normalise(emailAddress);

        return _sent.Where(m => m.EmailAddress == normalised).OrderBy(m => m.SentAt).LastOrDefault();
    }

    public IReadOnlyList<SignInLinkMessage> All() => [.. _sent];

    public void Clear() => _sent.Clear();
}
