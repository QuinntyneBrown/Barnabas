using System.Net;
using Barnabas.Domain.Access;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Barnabas.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Barnabas.IntegrationTests.Access;

// Acceptance Test
// Traces to: L2-014, L2-015, L2-016
// Description: A sign-in link exchanges for a session bound to the member and their
// congregation, expires after fifteen minutes, and works exactly once.
public sealed class ExchangeASignInLinkTests : AcceptanceTest
{
    public ExchangeASignInLinkTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-014 AC1: Given an unexpired, unused sign-in token, when it is exchanged, then a JWT is
    // issued carrying the member identifier, the congregation identifier, and the member's role.
    // L2-014 AC4: its congregation claim matches the congregation on the member record and not
    // any value supplied by the caller.
    // L2-014 AC5: it carries a session identifier.
    [Fact]
    public async Task A_valid_link_issues_a_token_bound_to_the_member_and_congregation()
    {
        using var client = Api.CreateClient();

        var session = await SignInAsync(client, SeedData.Priya.EmailAddress);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(session.AccessToken);

        token.GetClaim(BarnabasClaims.MemberId).Value.ShouldBe(SeedData.Priya.Id.ToString());
        token.GetClaim(BarnabasClaims.CongregationId).Value.ShouldBe(SeedData.StAidans.Id.ToString());
        token.GetClaim(BarnabasClaims.Role).Value.ShouldBe("Member");
        token.GetClaim(BarnabasClaims.SessionId).Value.ShouldBe(session.SessionId.ToString());
    }

    // L2-014 AC2: Given an issued JWT, when it is presented to an authenticated endpoint, then
    // the request succeeds as that member.
    [Fact]
    public async Task An_issued_token_is_accepted_as_that_member()
    {
        using var client = Api.CreateClient();

        var session = await SignInAsync(client, SeedData.Priya.EmailAddress);

        using var authenticated = Api.CreateClient();
        authenticated.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await authenticated.GetAsync("/board", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // L2-014 AC5: a token carrying no session identifier is refused.
    [Fact]
    public async Task A_token_naming_no_session_is_refused()
    {
        using var client = Api.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ForgedTokens.WithoutSession());

        var response = await client.GetAsync("/board", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-015 AC1: Given a sign-in token issued 16 minutes ago, when it is exchanged, then the
    // response is 410 Gone and no session is created.
    [Fact]
    public async Task A_link_older_than_fifteen_minutes_is_gone()
    {
        using var client = Api.CreateClient();

        var token = await RequestLinkAsync(client, SeedData.Priya.EmailAddress);

        Api.Clock.Advance(TimeSpan.FromMinutes(16));

        var response = await client.PostJsonAsync(
            "/sessions",
            new { token },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Gone);
        (await CountSessionsAsync()).ShouldBe(0);
    }

    // L2-015 AC2: Given a sign-in token issued 1 minute ago, when it is exchanged, then a
    // session is created.
    [Fact]
    public async Task A_link_a_minute_old_still_works()
    {
        using var client = Api.CreateClient();

        var token = await RequestLinkAsync(client, SeedData.Priya.EmailAddress);

        Api.Clock.Advance(TimeSpan.FromMinutes(1));

        var response = await client.PostJsonAsync(
            "/sessions",
            new { token },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CountSessionsAsync()).ShouldBe(1);
    }

    // L2-016 AC1: Given a sign-in token already exchanged once, when it is exchanged again, then
    // the response is 410 Gone and no second session is created.
    [Fact]
    public async Task A_link_works_exactly_once()
    {
        using var client = Api.CreateClient();

        var token = await RequestLinkAsync(client, SeedData.Priya.EmailAddress);

        var first = await client.PostJsonAsync("/sessions", new { token }, TestContext.Current.CancellationToken);
        var second = await client.PostJsonAsync("/sessions", new { token }, TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.Gone);
        (await CountSessionsAsync()).ShouldBe(1);
    }

    // L2-016 AC2: Given a sign-in token exchanged concurrently by two callers, when both
    // complete, then exactly one session is created.
    [Fact]
    public async Task Two_callers_racing_one_link_produce_one_session()
    {
        using var client = Api.CreateClient();

        var token = await RequestLinkAsync(client, SeedData.Priya.EmailAddress);

        var responses = await Task.WhenAll(
            client.PostJsonAsync("/sessions", new { token }, TestContext.Current.CancellationToken),
            client.PostJsonAsync("/sessions", new { token }, TestContext.Current.CancellationToken));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Gone).ShouldBe(1);
        (await CountSessionsAsync()).ShouldBe(1);
    }

    private async Task<string> RequestLinkAsync(HttpClient client, string emailAddress)
    {
        var response = await client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        return Api.Outbox.LatestFor(emailAddress)!.Token;
    }

    private async Task<SessionBody> SignInAsync(HttpClient client, string emailAddress)
    {
        var token = await RequestLinkAsync(client, emailAddress);

        var response = await client.PostJsonAsync("/sessions", new { token }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<SessionBody>();
    }

    private Task<int> CountSessionsAsync() =>
        Api.QueryAsync(context => context.Set<Session>().IgnoreQueryFilters().CountAsync());
}
