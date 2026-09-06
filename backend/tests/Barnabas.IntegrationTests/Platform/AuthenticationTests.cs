using System.Net;
using System.Net.Http.Headers;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Platform;

// Acceptance Test
// Traces to: L2-093
// Description: Everything but the public landing, invite redemption, and sign-in demands a
// valid token, and a token that is malformed, unsigned, or signed with the wrong key is not one.
public sealed class AuthenticationTests : AcceptanceTest
{
    /// <summary>
    /// Every route feature slice 1 exposes that is not one of the three public ones.
    /// </summary>
    /// <remarks>
    /// Listed rather than discovered so that a new endpoint has to be added here deliberately.
    /// A test that enumerated the route table would grow a hole the moment somebody wrote an
    /// endpoint and forgot about authentication, which is the failure it exists to catch.
    /// </remarks>
    public static TheoryData<string, string> ProtectedRoutes() => new()
    {
        { "GET", "/board" },
        { "GET", "/listings/mine" },
        { "GET", $"/listings/{SeedData.Listings.Ladder}" },
        { "POST", "/listings/lend" },
        { "POST", $"/listings/{SeedData.Listings.Ladder}/close-out" },
        { "POST", $"/listings/{SeedData.Listings.Ladder}/requests/loan" },
        { "GET", "/requests/incoming" },
        { "GET", "/requests/mine" },
        { "POST", $"/requests/{Guid.Empty}/accept" },
        { "POST", $"/requests/{Guid.Empty}/decline" },
        { "GET", "/threads" },
        { "GET", $"/threads/{Guid.Empty}" },
        { "POST", $"/threads/{Guid.Empty}/messages" },
        { "DELETE", "/sessions" },
    };

    public AuthenticationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-093 AC1: Given no credentials, when any endpoint other than the public ones is called,
    // then the response is 401 Unauthorized.
    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task An_endpoint_refuses_a_caller_with_no_credentials(string method, string route)
    {
        using var client = Api.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = Json.From.Of(new { }),
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // The three public endpoints stay public, or a member could never sign in at all.
    [Fact]
    public async Task Signing_in_needs_no_credentials()
    {
        using var client = Api.CreateClient();

        var health = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        var link = await client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = SeedData.Priya.EmailAddress },
            TestContext.Current.CancellationToken);

        health.StatusCode.ShouldBe(HttpStatusCode.OK);
        link.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // L2-093 AC2: Given a malformed or unsigned token, when it is presented, then 401.
    [Fact]
    public async Task A_malformed_token_is_refused()
    {
        (await ReadBoardWithAsync(ForgedTokens.Malformed)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-093 AC3: Given a token signed with the wrong key, when it is presented, then 401.
    [Fact]
    public async Task A_token_signed_with_the_wrong_key_is_refused()
    {
        (await ReadBoardWithAsync(ForgedTokens.WithWrongKey())).ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpStatusCode> ReadBoardWithAsync(string token)
    {
        using var client = Api.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/board", TestContext.Current.CancellationToken);

        return response.StatusCode;
    }
}
