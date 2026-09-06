namespace Barnabas.Application.Common.Email;

/// <summary>
/// Delivers the one message Barnabas sends in feature slice 1.
/// </summary>
/// <remarks>
/// An abstraction rather than a mail client so delivery is swappable and the acceptance suite
/// needs no mail server. The handler passes the token; composing it into a URL is the
/// implementation's business, because the address of the web client is not something the
/// application layer should know.
/// </remarks>
public interface IEmailSender
{
    Task SendSignInLinkAsync(string emailAddress, string token, CancellationToken cancellationToken);
}
