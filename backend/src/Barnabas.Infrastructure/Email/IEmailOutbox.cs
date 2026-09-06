namespace Barnabas.Infrastructure.Email;

/// <summary>
/// What was dispatched, for a caller that has no mailbox to look in.
/// </summary>
/// <remarks>
/// This exists so the acceptance suites can drive the real sign-in flow rather than a
/// simulation of it. The integration tests read it through dependency injection; the Playwright
/// suite reads it through an endpoint that is registered only in Development.
/// </remarks>
public interface IEmailOutbox
{
    SignInLinkMessage? LatestFor(string emailAddress);

    IReadOnlyList<SignInLinkMessage> All();

    void Clear();
}
