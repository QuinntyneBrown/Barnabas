using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Access;

// Acceptance Test
// Traces to: L2-018
// Description: A session expires, renews without another trip to the inbox, rotates its refresh
// token on use, and is independent of the member's other sessions.
public sealed class RenewASessionTests : AcceptanceTest
{
    public RenewASessionTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-018 AC1: Given a JWT past its expiry, when it is presented, then the response is 401.
    [Fact]
    public async Task An_expired_token_is_refused()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);

        Api.Clock.Advance(TimeSpan.FromMinutes(16));

        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-018 AC2: Given a valid, unexpired session, when renewal is requested, then a new token
    // is issued and the previous refresh token is invalidated.
    [Fact]
    public async Task Renewal_issues_a_new_token_and_retires_the_old_one()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        var previousRefreshToken = device.RefreshToken;
        var previousAccessToken = device.AccessToken;

        Api.Clock.Advance(TimeSpan.FromMinutes(5));

        (await device.RenewAsync()).ShouldBe(HttpStatusCode.OK);

        device.AccessToken.ShouldNotBe(previousAccessToken);
        device.RefreshToken.ShouldNotBe(previousRefreshToken);

        // Presenting the retired token is evidence of a copy, and is refused.
        (await device.RenewWithAsync(previousRefreshToken)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-018 AC4: Given one refresh token presented concurrently by two callers, when both
    // complete, then exactly one rotation succeeds and the other receives 401.
    [Fact]
    public async Task Two_callers_racing_one_refresh_token_produce_one_winner()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        var token = device.RefreshToken;

        var outcomes = await Task.WhenAll(
            device.RenewWithAsync(token),
            device.RenewWithAsync(token));

        outcomes.Count(status => status == HttpStatusCode.OK).ShouldBe(1);
        outcomes.Count(status => status == HttpStatusCode.Unauthorized).ShouldBe(1);
    }

    // L2-018 AC5: Given a member holding two active sessions, when one is renewed, then the
    // other remains usable.
    [Fact]
    public async Task Renewing_one_session_leaves_the_other_alone()
    {
        var phone = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);
        var laptop = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        (await phone.RenewAsync()).ShouldBe(HttpStatusCode.OK);

        (await laptop.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);
        (await phone.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);
    }
}
