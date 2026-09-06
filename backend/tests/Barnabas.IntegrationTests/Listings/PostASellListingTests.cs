using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-029, L2-034
// Description: A member offers something for sale. The price is a stated asking figure in
// Canadian dollars, settled between the members; Barnabas never takes payment for it.
public sealed class PostASellListingTests : AcceptanceTest
{
    public PostASellListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-029 AC1: Given a signed-in member, when they post a Sell listing with a price of
    // 45.00, then it is created with kind Sell and that price.
    [Fact]
    public async Task A_sell_listing_is_created_with_its_asking_price()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync("/listings/sell", ABicycle(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.ReadAsync<PostedListing>();

        var board = await client.GetAsync("/board", TestContext.Current.CancellationToken);
        var page = await board.ReadAsync<BoardPageBody>();

        var posted = page.Listings.SingleOrDefault(listing => listing.ListingId == created.ListingId);

        posted.ShouldNotBeNull();
        posted.Kind.ShouldBe(nameof(ListingKind.Sell));
        posted.Price.ShouldBe(45.00m);
    }

    // L2-029 AC2: Given a Sell listing submitted without a price, when it is posted, then the
    // response is 400 Bad Request naming the field.
    [Fact]
    public async Task A_sell_listing_without_a_price_is_rejected_naming_the_price()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/sell",
            ABicycle(price: null),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Price");
    }

    // L2-029 AC3: Given a Sell listing submitted with a negative price, when it is posted, then
    // the response is 400 Bad Request.
    [Fact]
    public async Task A_negative_price_is_rejected()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/sell",
            ABicycle(price: -1.00m),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Price");
    }

    // L2-029: a Sell listing carries a condition. Without it the price means little, so it is
    // required rather than optional.
    [Fact]
    public async Task A_sell_listing_without_a_condition_is_rejected_naming_the_condition()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/sell",
            ABicycle(condition: string.Empty),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Condition");
    }

    // L2-034 AC1: Given the published API surface, when it is enumerated, then no endpoint
    // accepts payment instrument details.
    //
    // Narrower than the requirement by design: this states it of the endpoint that has the only
    // reason to want one. The whole-surface sweep belongs with the other kinds' request
    // endpoints and is asserted in RequestsDoNotTakePaymentTests.
    [Fact]
    public async Task A_payment_instrument_is_not_accepted_when_posting_a_sale()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var before = await CountListingsAsync();

        var response = await client.PostRawJsonAsync(
            "/listings/sell",
            """
            {
              "title": "Raleigh three-speed",
              "description": "Rides well, new tyres last spring.",
              "category": "Outdoors",
              "neighbourhood": "Riverdale",
              "condition": "Good",
              "price": 45.00,
              "cardNumber": "4111111111111111"
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("cardNumber");

        (await CountListingsAsync()).ShouldBe(before);
    }

    private static object ABicycle(
        decimal? price = 45.00m,
        string? condition = null) => new
    {
        title = "Raleigh three-speed",
        description = "Rides well, new tyres last spring.",
        category = "Outdoors",
        neighbourhood = SeedData.Marion.Neighbourhood,
        condition = condition ?? "Good",
        price,
    };

    private Task<int> CountListingsAsync() =>
        Api.QueryAsync(context => context.Set<Listing>().IgnoreQueryFilters().CountAsync());
}
