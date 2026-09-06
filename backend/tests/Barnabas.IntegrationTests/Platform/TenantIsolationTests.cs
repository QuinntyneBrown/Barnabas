using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Platform;

// Acceptance Test
// Traces to: L2-088 (partial - AC1, AC3, AC4), L2-089
// Description: A member reads only their own congregation, and a resource belonging to another
// answers exactly as one that never existed.
//
// L2-088 AC2 names search, which is L1-008 and out of feature slice 1, so that criterion is not
// covered here and the requirement stands as partially delivered.
public sealed class TenantIsolationTests : AcceptanceTest
{
    public TenantIsolationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-088 AC1: Given congregations A and B each holding listings, when a member of A requests
    // the board, then no listing of B is returned.
    [Fact]
    public async Task The_board_holds_nothing_from_another_congregation()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        var aidans = await (await priya.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        var brigids = await (await hank.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        aidans.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Canoe);
        brigids.Listings.Select(listing => listing.ListingId).ShouldBe([SeedData.Listings.Canoe]);
    }

    // L2-088 AC3: Given a token whose congregation claim is altered, when it is presented, then
    // signature validation fails and the response is 401.
    [Fact]
    public async Task A_tampered_congregation_claim_breaks_the_signature()
    {
        using var client = Api.CreateClient();

        var token = await Api.SignInAsync(SeedData.Priya.Id);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TamperedTokens.WithCongregation(token, SeedData.StBrigids.Id));

        var response = await client.GetAsync("/board", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-088 AC4: Given a request carrying no congregation context, when it attempts to read
    // listings, requests, threads, or members, then the read is refused rather than returning
    // unfiltered rows.
    [Fact]
    public async Task An_unauthenticated_read_is_refused_rather_than_unfiltered()
    {
        using var client = Api.CreateClient();

        foreach (var route in new[] { "/board", "/listings/mine", "/requests/incoming", "/requests/mine", "/threads" })
        {
            var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{route} answered without a congregation");
            (await response.ReadBodyAsync()).ShouldNotContain(SeedData.Listings.LadderTitle);
        }
    }

    // L2-089 AC1 and AC2: a listing in another congregation, and an identifier that exists
    // nowhere, are both 404 - and indistinguishable from each other.
    [Fact]
    public async Task A_listing_in_another_congregation_is_not_found_and_looks_like_nothing_at_all()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var elsewhere = await priya.GetAsync(
            $"/listings/{SeedData.Listings.Canoe}",
            TestContext.Current.CancellationToken);

        var nowhere = await priya.GetAsync(
            $"/listings/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        elsewhere.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        nowhere.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Indistinguishable, not merely both 404. A difference in the body would disclose
        // exactly what the status code is being careful not to. The trace identifier is set
        // aside: it changes on every request, and says nothing about the resource.
        WithoutTraceId(await elsewhere.ReadBodyAsync())
            .ShouldBe(WithoutTraceId(await nowhere.ReadBodyAsync()));
    }

    private static string WithoutTraceId(string body) =>
        System.Text.RegularExpressions.Regex.Replace(body, "\"traceId\":\"[^\"]*\"", "\"traceId\":\"*\"");

    // L2-089 AC1 again, for a write: a cross-congregation listing cannot be closed out either,
    // and the answer is still not found rather than forbidden.
    [Fact]
    public async Task A_write_against_another_congregation_is_not_found()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Canoe}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
