using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-037
// Description: An owner closes out a listing and it leaves the board carrying the outcome its
// own kind gives it - a loan is Archived, a sale is Sold, a gift is Given away, an offer of
// time is Completed.
//
// The four outcomes are one transition read four ways. CloseOut takes no status argument: the
// entity reads its own kind, so no caller can mark a gift sold.
public sealed class CloseOutAListingTests : AcceptanceTest
{
    public CloseOutAListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-037 AC3: Given an active Lend listing, when its owner marks it taken, then its status
    // becomes Archived.
    [Fact]
    public async Task A_lend_listing_closes_out_as_archived_and_leaves_the_board()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var closed = await response.ReadAsync<ClosedOutListing>();

        // Archived, not a generic closed state. The four outcomes stay distinguishable so that a
        // member reading last spring can see what became of each thing.
        closed.Status.ShouldBe(nameof(ListingStatus.Archived));
        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Archived);

        var board = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // A listing is closed out once. The second attempt is reported rather than ignored.
    [Fact]
    public async Task A_listing_already_closed_out_answers_conflict()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        var second = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // L2-037 AC2: Given an active Give listing, when its owner marks it taken, then its status
    // becomes Given away.
    [Fact]
    public async Task A_give_listing_closes_out_as_given_away()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(marion, "give", new
        {
            title = "Wooden high chair",
            description = "Our youngest has outgrown it.",
            category = "Household",
            neighbourhood = SeedData.Marion.Neighbourhood,
        });

        (await CloseOutAsync(marion, listingId)).Status.ShouldBe(nameof(ListingStatus.GivenAway));
        (await StatusOfAsync(listingId)).ShouldBe(ListingStatus.GivenAway);
    }

    // L2-037 AC1: Given an active Sell listing, when its owner marks it sold, then its status
    // becomes Sold.
    [Fact]
    public async Task A_sell_listing_closes_out_as_sold()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(marion, "sell", new
        {
            title = "Raleigh three-speed",
            description = "Rides well.",
            category = "Outdoors",
            neighbourhood = SeedData.Marion.Neighbourhood,
            condition = "Good",
            price = 45.00m,
        });

        (await CloseOutAsync(marion, listingId)).Status.ShouldBe(nameof(ListingStatus.Sold));
        (await StatusOfAsync(listingId)).ShouldBe(ListingStatus.Sold);
    }

    // L2-037 AC4: Given an active Help listing, when its owner marks it booked, then its status
    // becomes Completed.
    [Fact]
    public async Task A_help_listing_closes_out_as_completed()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(marion, "help", new
        {
            title = "Lifts to appointments",
            description = "Happy to drive within the east end.",
            category = "Rides",
            neighbourhood = SeedData.Marion.Neighbourhood,
            windows = new[]
            {
                new { day = nameof(DayOfWeek.Tuesday), startsAt = "07:30", endsAt = "10:30" },
            },
        });

        (await CloseOutAsync(marion, listingId)).Status.ShouldBe(nameof(ListingStatus.Completed));
        (await StatusOfAsync(listingId)).ShouldBe(ListingStatus.Completed);
    }

    private static async Task<Guid> PostAsync(HttpClient client, string kind, object body)
    {
        var response = await client.PostJsonAsync($"/listings/{kind}", body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<PostedListing>()).ListingId;
    }

    private static async Task<ClosedOutListing> CloseOutAsync(HttpClient client, Guid listingId)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{listingId}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<ClosedOutListing>();
    }

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(async context =>
            (await context.Set<Listing>().IgnoreQueryFilters().SingleAsync(l => l.Id == listingId)).Status);
}
