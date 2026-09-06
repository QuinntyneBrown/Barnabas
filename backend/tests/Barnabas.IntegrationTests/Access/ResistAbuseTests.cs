using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Barnabas.Infrastructure.Security;

namespace Barnabas.IntegrationTests.Access;

// Acceptance Test
// Traces to: L2-013, L2-017, L2-099
// Description: A mailbox cannot be flooded with sign-in links, a source cannot work its way
// through the invite alphabet, and neither limit says anything about whether an address is
// registered.
public sealed class ResistAbuseTests : AcceptanceTest
{
    public ResistAbuseTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-017 AC1: Given five sign-in link requests for one address within 15 minutes, when a sixth
    // is made, then the response is 429 Too Many Requests and no further link is dispatched.
    [Fact]
    public async Task A_sixth_sign_in_link_in_a_quarter_of_an_hour_is_refused()
    {
        using var client = Api.CreateClient();

        for (var attempt = 0; attempt < SignInLinkThrottle.Limit; attempt += 1)
        {
            (await AskForALinkAsync(client, SeedData.Priya.EmailAddress))
                .StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        Api.Outbox.Clear();

        var sixth = await AskForALinkAsync(client, SeedData.Priya.EmailAddress);

        sixth.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // Refused, and nothing sent. A limiter that still posted the link would protect the
        // server and not the mailbox, which is the thing being protected.
        Api.Outbox.LatestFor(SeedData.Priya.EmailAddress).ShouldBeNull();
    }

    // L2-017 AC2: Given a rate-limited address, when 15 minutes have elapsed, then a further
    // request succeeds.
    //
    // Counted against the injected clock rather than the framework's limiter, whose window is
    // replenished by a timer no test can advance. This is why the throttle is application state.
    [Fact]
    public async Task After_a_quarter_of_an_hour_the_address_may_ask_again()
    {
        using var client = Api.CreateClient();

        for (var attempt = 0; attempt < SignInLinkThrottle.Limit; attempt += 1)
        {
            await AskForALinkAsync(client, SeedData.Priya.EmailAddress);
        }

        (await AskForALinkAsync(client, SeedData.Priya.EmailAddress))
            .StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        Api.Clock.Advance(SignInLinkThrottle.Window + TimeSpan.FromMinutes(1));

        var afterTheWindow = await AskForALinkAsync(client, SeedData.Priya.EmailAddress);

        afterTheWindow.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Outbox.LatestFor(SeedData.Priya.EmailAddress).ShouldNotBeNull();
    }

    // The cap is per address rather than per sender, because what it defends is a mailbox. One
    // member being flooded must not stop another member signing in.
    [Fact]
    public async Task One_flooded_address_does_not_stop_another_member_signing_in()
    {
        using var client = Api.CreateClient();

        for (var attempt = 0; attempt <= SignInLinkThrottle.Limit; attempt += 1)
        {
            await AskForALinkAsync(client, SeedData.Priya.EmailAddress);
        }

        (await AskForALinkAsync(client, SeedData.Grace.EmailAddress))
            .StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // L2-013 AC1: Given an unregistered email address, when a sign-in link is requested, then the
    // response is 202 Accepted with the same body as for a registered address, and no link is
    // dispatched.
    //
    // The throttle counts before the address is looked up, so an unregistered address is limited
    // exactly as a registered one is. Throttling only the addresses that exist would make the
    // limiter itself the disclosure this forbids.
    [Fact]
    public async Task A_registered_and_an_unregistered_address_are_answered_alike()
    {
        using var client = Api.CreateClient();

        var registered = await AskForALinkAsync(client, SeedData.Grace.EmailAddress);
        var unregistered = await AskForALinkAsync(client, "nobody@example.com");

        registered.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        unregistered.StatusCode.ShouldBe(registered.StatusCode);

        (await unregistered.ReadBodyAsync()).ShouldBe(await registered.ReadBodyAsync());

        Api.Outbox.LatestFor("nobody@example.com").ShouldBeNull();
    }

    [Fact]
    public async Task An_unregistered_address_is_throttled_the_same_way()
    {
        using var client = Api.CreateClient();

        for (var attempt = 0; attempt < SignInLinkThrottle.Limit; attempt += 1)
        {
            (await AskForALinkAsync(client, "nobody@example.com"))
                .StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        (await AskForALinkAsync(client, "nobody@example.com"))
            .StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // L2-099 AC1: Given a source exceeding the documented request rate, when a further request is
    // made, then the response is 429 Too Many Requests carrying a Retry-After header.
    [Fact]
    public async Task A_refusal_says_when_to_come_back()
    {
        using var client = Api.CreateClient();

        for (var attempt = 0; attempt <= SignInLinkThrottle.Limit; attempt += 1)
        {
            await AskForALinkAsync(client, SeedData.Priya.EmailAddress);
        }

        var refused = await AskForALinkAsync(client, SeedData.Priya.EmailAddress);

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // A 429 that does not say when leaves a client guessing, and a guessing client retries
        // sooner than it should.
        refused.Headers.RetryAfter.ShouldNotBeNull();
    }

    // L2-099 AC2: Given repeated failed invite redemptions from one source, when the threshold is
    // exceeded, then further attempts return 429 Too Many Requests.
    [Fact]
    public async Task A_source_guessing_at_invite_codes_is_stopped()
    {
        using var stranger = Api.CreateClient();

        for (var attempt = 0; attempt < RedemptionThrottle.Limit; attempt += 1)
        {
            var guess = await stranger.PostJsonAsync(
                "/invites/redeem",
                new { code = $"GUESS{attempt:D3}" },
                TestContext.Current.CancellationToken);

            guess.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        var stopped = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code = "GUESS999" },
            TestContext.Current.CancellationToken);

        stopped.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        stopped.Headers.RetryAfter.ShouldNotBeNull();
    }

    // Only failures count. Somebody redeeming their own code correctly never meets the limit.
    [Fact]
    public async Task A_correct_redemption_is_not_counted_against_the_guesser_limit()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var stranger = Api.CreateClient();

        for (var attempt = 0; attempt < RedemptionThrottle.Limit - 1; attempt += 1)
        {
            await stranger.PostJsonAsync(
                "/invites/redeem",
                new { code = $"GUESS{attempt:D3}" },
                TestContext.Current.CancellationToken);
        }

        var issued = await marion.PostJsonAsync("/invites", new { }, TestContext.Current.CancellationToken);
        var code = (await issued.ReadAsync<IssuedInviteCode>()).Code;

        var redeemed = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code },
            TestContext.Current.CancellationToken);

        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Task<HttpResponseMessage> AskForALinkAsync(HttpClient client, string emailAddress) =>
        client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress },
            TestContext.Current.CancellationToken);
}
