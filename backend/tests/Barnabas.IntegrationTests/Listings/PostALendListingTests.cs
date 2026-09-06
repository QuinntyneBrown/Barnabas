using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-027, L2-031
// Description: A member posts a Lend listing with the fields its kind needs, it reaches the
// board, and a price on it is rejected rather than quietly dropped.
public sealed class PostALendListingTests : AcceptanceTest
{
    public PostALendListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-027 AC1: Given a signed-in member, when they post a Lend listing with a title,
    // description, category, neighbourhood, and return-by period, then it is created with kind
    // Lend and appears on the board.
    [Fact]
    public async Task A_lend_listing_is_created_and_reaches_the_board()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync("/listings/lend", ALadder(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.ReadAsync<PostedListing>();

        var board = await client.GetAsync("/board", TestContext.Current.CancellationToken);
        var page = await board.ReadAsync<BoardPageBody>();

        var posted = page.Listings.SingleOrDefault(listing => listing.ListingId == created.ListingId);

        posted.ShouldNotBeNull();
        posted.Kind.ShouldBe(nameof(ListingKind.Lend));
        posted.Title.ShouldBe("Extending ladder, three sections");
        posted.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        posted.Neighbourhood.ShouldBe(SeedData.Marion.Neighbourhood);
        posted.Price.ShouldBeNull();
    }

    // L2-027 AC2: Given a Lend listing submitted with a price, when it is posted, then the
    // response is 400 Bad Request.
    [Fact]
    public async Task A_price_on_a_lend_listing_is_rejected_by_name()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostRawJsonAsync(
            "/listings/lend",
            """
            {
              "title": "Extending ladder, three sections",
              "description": "Sound, both spreaders lock.",
              "category": "Tools",
              "neighbourhood": "Riverdale",
              "returnBy": "2026-10-01",
              "price": 45.00
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Named, not silently dropped. The command has no property to bind a price to, so
        // ignoring it would have answered 201 and made the requirement unimplementable.
        (await response.ReadInvalidFieldsAsync()).ShouldContain("price");

        (await CountListingsAsync()).ShouldBe(3);
    }

    // L2-031 AC1: Given a listing with a title longer than 120 characters, when it is posted,
    // then the response is 400 Bad Request naming the title.
    [Fact]
    public async Task An_over_long_title_is_rejected_naming_the_title()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/lend",
            ALadder(title: new string('a', Listing.TitleMaxLength + 1)),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Title");
    }

    // L2-031 AC2: Given a listing with an empty title, when it is posted, then the response is
    // 400 Bad Request naming the title.
    [Fact]
    public async Task An_empty_title_is_rejected_naming_the_title()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/lend",
            ALadder(title: string.Empty),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Title");
    }

    // L2-031 AC3: Given a listing with a description longer than 4000 characters, when it is
    // posted, then the response is 400 Bad Request naming the description.
    [Fact]
    public async Task An_over_long_description_is_rejected_naming_the_description()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/lend",
            ALadder(description: new string('a', Listing.DescriptionMaxLength + 1)),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Description");
    }

    private static object ALadder(string? title = null, string? description = null) => new
    {
        title = title ?? "Extending ladder, three sections",
        description = description ?? "Sound, both spreaders lock. Kept in the garage.",
        category = "Tools",
        neighbourhood = SeedData.Marion.Neighbourhood,
        returnBy = "2026-10-01",
    };

    private Task<int> CountListingsAsync() =>
        Api.QueryAsync(context => context.Set<Listing>().IgnoreQueryFilters().CountAsync());
}
