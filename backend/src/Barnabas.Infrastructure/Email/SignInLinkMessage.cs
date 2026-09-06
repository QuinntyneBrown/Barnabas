namespace Barnabas.Infrastructure.Email;

/// <summary>One dispatched sign-in link, as the outbox remembers it.</summary>
public sealed record SignInLinkMessage(string EmailAddress, string Token, string Url, DateTimeOffset SentAt);
