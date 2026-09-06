using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-028
// Description: A member gives something away. Ownership transfers and pickup is arranged
// between the members, so the listing carries no price and a submitted one is refused.
public sealed class PostAGiveListingTests : AcceptanceTest
{
    public PostAGiveListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-028 AC1: Given a signed-in member, when they post a Give listing with the required
    // fields, then it is created with kind Give and no price.
    [Fact]
    public async Task A_give_listing_is_created_with_no_price()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync("/listings/give", AHighChair(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.ReadAsync<PostedListing>();

        var board = await client.GetAsync("/board", TestContext.Current.CancellationToken);
        var page = await board.ReadAsync<BoardPageBody>();

        var posted = page.Listings.SingleOrDefault(listing => listing.ListingId == created.ListingId);

        posted.ShouldNotBeNull();
        posted.Kind.ShouldBe(nameof(ListingKind.Give));
        posted.Title.ShouldBe("Wooden high chair");
        posted.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        posted.Price.ShouldBeNull();
    }

    // L2-028 AC2: Given a Give listing submitted with a price, when it is posted, then the
    // response is 400 Bad Request.
    [Fact]
    public async Task A_price_on_a_give_listing_is_rejected_by_name()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var before = await CountListingsAsync();

        var response = await client.PostRawJsonAsync(
            "/listings/give",
            """
            {
              "title": "Wooden high chair",
              "description": "Our youngest has outgrown it. Wipes clean.",
              "category": "Household",
              "neighbourhood": "Riverdale",
              "price": 20.00
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // A gift with a price is a sale. The command has nowhere to bind one, so without the
        // forbidden-field declaration this would have answered 201 and quietly dropped it.
        (await response.ReadInvalidFieldsAsync()).ShouldContain("price");

        (await CountListingsAsync()).ShouldBe(before);
    }

    private static object AHighChair() => new
    {
        title = "Wooden high chair",
        description = "Our youngest has outgrown it. Wipes clean.",
        category = "Household",
        neighbourhood = SeedData.Marion.Neighbourhood,
    };

    private Task<int> CountListingsAsync() =>
        Api.QueryAsync(context => context.Set<Listing>().IgnoreQueryFilters().CountAsync());
}
