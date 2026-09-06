using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-026
// Description: Posting begins with choosing a kind. The kind is the route, so it cannot be
// defaulted, and a submission that names no kind is refused rather than guessed at.
public sealed class ChooseAKindTests : AcceptanceTest
{
    public ChooseAKindTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-026 AC4: Given a listing submitted without a kind, when it is posted, then the response
    // is 400 Bad Request naming the field.
    [Fact]
    public async Task A_listing_posted_without_a_kind_is_rejected_naming_the_kind()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings",
            new
            {
                title = "Extending ladder, three sections",
                description = "Sound, both spreaders lock.",
                category = "Tools",
                neighbourhood = SeedData.Marion.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Named, so the member is told what is missing rather than being handed a listing of
        // whichever kind the server felt like.
        (await response.ReadInvalidFieldsAsync()).ShouldContain("kind");
    }

    [Theory]
    [InlineData("lend")]
    [InlineData("give")]
    [InlineData("sell")]
    [InlineData("help")]
    public async Task Every_kind_has_a_route_of_its_own(string kind)
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        // An empty body, so this reaches validation rather than routing. A kind without a route
        // would answer 404 or 405 here instead.
        var response = await client.PostJsonAsync(
            $"/listings/{kind}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
