using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-030
// Description: A member offers time rather than a thing. Time has to be offered when the
// member is actually free, so a Help listing declares the windows it is available in, and
// carries neither a price nor a photo.
public sealed class PostAHelpListingTests : AcceptanceTest
{
    public PostAHelpListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-030 AC1: Given a signed-in member, when they post a Help listing with two availability
    // windows, then it is created with kind Help and both windows are returned.
    [Fact]
    public async Task A_help_listing_is_created_carrying_both_its_windows()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync("/listings/help", ARide(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.ReadAsync<PostedListing>();

        var detail = await client.GetAsync($"/listings/{created.ListingId}", TestContext.Current.CancellationToken);
        var listing = await detail.ReadAsync<ListingDetailBody>();

        listing.Kind.ShouldBe(nameof(ListingKind.Help));
        listing.Price.ShouldBeNull();

        listing.AvailabilityWindows.ShouldNotBeNull();
        listing.AvailabilityWindows.Count.ShouldBe(2);

        listing.AvailabilityWindows[0].Day.ShouldBe(nameof(DayOfWeek.Tuesday));
        listing.AvailabilityWindows[0].StartsAt.ShouldBe(new TimeOnly(7, 30));
        listing.AvailabilityWindows[0].EndsAt.ShouldBe(new TimeOnly(10, 30));

        listing.AvailabilityWindows[1].Day.ShouldBe(nameof(DayOfWeek.Thursday));

        // Each window is identified, because a request has to name the one it wants.
        listing.AvailabilityWindows.Select(window => window.AvailabilityWindowId)
            .ShouldBeUnique();
    }

    // L2-030 AC2: Given a Help listing submitted with no availability window, when it is posted,
    // then the response is 400 Bad Request.
    [Fact]
    public async Task A_help_listing_with_no_window_is_rejected_naming_the_windows()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/help",
            ARide(windows: []),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Windows");
    }

    // L2-030 AC3: Given a Help listing submitted with a price, when it is posted, then the
    // response is 400 Bad Request.
    [Fact]
    public async Task A_price_on_a_help_listing_is_rejected_by_name()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var before = await CountListingsAsync();

        var response = await client.PostRawJsonAsync(
            "/listings/help",
            """
            {
              "title": "Lifts to appointments",
              "description": "Happy to drive within the east end.",
              "category": "Rides",
              "neighbourhood": "Riverdale",
              "windows": [ { "day": "Tuesday", "startsAt": "07:30", "endsAt": "10:30" } ],
              "price": 20.00
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Help offers time. Charging for it would make it a sale, and the kind carries no price
        // for the same reason a Give listing does not.
        (await response.ReadInvalidFieldsAsync()).ShouldContain("price");

        (await CountListingsAsync()).ShouldBe(before);
    }

    // L2-030: a window that ends before it starts is not a window.
    [Fact]
    public async Task A_window_ending_before_it_starts_is_rejected()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/help",
            ARide(windows: [new { day = nameof(DayOfWeek.Tuesday), startsAt = "10:30", endsAt = "07:30" }]),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Windows");
    }

    private static object ARide(object[]? windows = null) => new
    {
        title = "Lifts to appointments",
        description = "Happy to drive within the east end.",
        category = "Rides",
        neighbourhood = SeedData.Marion.Neighbourhood,
        windows = windows ??
        [
            new { day = nameof(DayOfWeek.Tuesday), startsAt = "07:30", endsAt = "10:30" },
            new { day = nameof(DayOfWeek.Thursday), startsAt = "13:00", endsAt = "16:30" },
        ],
    };

    private Task<int> CountListingsAsync() =>
        Api.QueryAsync(context => context.Set<Listing>().IgnoreQueryFilters().CountAsync());
}
