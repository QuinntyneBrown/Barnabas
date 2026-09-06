using System.Net;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Requests;

// Acceptance Test
// Traces to: L2-055, L2-056, L2-057, L2-063
// Description: Each kind of listing is asked for on its own endpoint, collecting the terms that
// kind needs and refusing the ones Barnabas does not handle. A gift and a sale need a pickup;
// help needs one of the windows the offer declared; none of them takes money.
public sealed class RequestEveryKindTests : AcceptanceTest
{
    private const string PickupAt = "2026-10-01T14:00:00+00:00";

    public RequestEveryKindTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-055 AC1: Given an active Give listing, when a member submits a request with a message
    // and pickup time, then a request is created with status Pending.
    [Fact]
    public async Task A_gift_request_is_created_pending()
    {
        var listingId = await MarionPostsAsync("give", AHighChair());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{listingId}/requests/gift",
            new { message = "We are expecting in March and would be glad of it.", pickupAt = PickupAt },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await StatusOfAsync(priya, listingId)).ShouldBe(nameof(RequestStatus.Pending));
    }

    // L2-056 AC1: Given an active Sell listing, when a member submits a request with a message
    // and pickup time, then a request is created with status Pending.
    [Fact]
    public async Task A_purchase_request_is_created_pending()
    {
        var listingId = await MarionPostsAsync("sell", ABicycle());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{listingId}/requests/purchase",
            new { message = "Is it still available? I could come Saturday.", pickupAt = PickupAt },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await StatusOfAsync(priya, listingId)).ShouldBe(nameof(RequestStatus.Pending));
    }

    // L2-056 AC3: Given a Sell request, when it is submitted, then no payment instrument field
    // is accepted.
    [Fact]
    public async Task A_purchase_request_refuses_a_payment_instrument()
    {
        var listingId = await MarionPostsAsync("sell", ABicycle());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostRawJsonAsync(
            $"/listings/{listingId}/requests/purchase",
            $$"""
            {
              "message": "Is it still available?",
              "pickupAt": "{{PickupAt}}",
              "cardNumber": "4111111111111111"
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("cardNumber");
    }

    // L2-057 AC1: Given a Help listing declaring two availability windows, when a member submits
    // a request choosing one of them, then a request is created with status Pending carrying
    // that window.
    [Fact]
    public async Task A_help_request_carries_the_window_it_chose()
    {
        var listingId = await MarionPostsAsync("help", ARide());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var listing = await (await priya.GetAsync($"/listings/{listingId}", TestContext.Current.CancellationToken))
            .ReadAsync<ListingDetailBody>();

        listing.AvailabilityWindows.ShouldNotBeNull();
        var chosen = listing.AvailabilityWindows[1];

        var response = await priya.PostJsonAsync(
            $"/listings/{listingId}/requests/help",
            new
            {
                message = "My mother has an appointment that morning.",
                availabilityWindowId = chosen.AvailabilityWindowId,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var mine = await (await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        var made = mine.Single(request => request.ListingId == listingId);

        made.Status.ShouldBe(nameof(RequestStatus.Pending));

        // The chosen window is shown back in the offer's own words, so the requester can see
        // which one they asked for without holding an identifier.
        made.Terms.ShouldContain(chosen.Day);
    }

    // L2-057 AC2: Given a Help listing, when a request is submitted choosing a window the
    // listing did not declare, then the response is 400 Bad Request.
    [Fact]
    public async Task A_help_request_naming_an_undeclared_window_is_rejected()
    {
        var listingId = await MarionPostsAsync("help", ARide());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{listingId}/requests/help",
            new { message = "Could you manage Monday?", availabilityWindowId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("AvailabilityWindowId");
    }

    // L2-062: the endpoint has to match the listing's kind. Nothing else ties the two together,
    // so without this a Give listing could acquire a request meant for a sale.
    [Fact]
    public async Task Asking_for_a_gift_on_the_purchase_endpoint_is_refused()
    {
        var listingId = await MarionPostsAsync("give", AHighChair());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{listingId}/requests/purchase",
            new { message = "I would like it.", pickupAt = PickupAt },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // L2-063 AC1: Given the request endpoints, when their accepted fields are enumerated, then
    // none accepts a payment instrument, delivery address, or deposit amount.
    [Theory]
    [InlineData("cardNumber", "4111111111111111")]
    [InlineData("deliveryAddress", "12 Bain Avenue")]
    [InlineData("deposit", "20.00")]
    public async Task No_request_endpoint_accepts_payment_delivery_or_a_deposit(string field, string value)
    {
        var listingId = await MarionPostsAsync("give", AHighChair());

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostRawJsonAsync(
            $"/listings/{listingId}/requests/gift",
            $$"""
            {
              "message": "We would be glad of it.",
              "pickupAt": "{{PickupAt}}",
              "{{field}}": "{{value}}"
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain(field);
    }

    private static async Task<string> StatusOfAsync(HttpClient client, Guid listingId)
    {
        var mine = await (await client.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        return mine.Single(request => request.ListingId == listingId).Status;
    }

    private static object AHighChair() => new
    {
        title = "Wooden high chair",
        description = "Our youngest has outgrown it.",
        category = "Household",
        neighbourhood = SeedData.Marion.Neighbourhood,
    };

    private static object ABicycle() => new
    {
        title = "Raleigh three-speed",
        description = "Rides well.",
        category = "Outdoors",
        neighbourhood = SeedData.Marion.Neighbourhood,
        condition = "Good",
        price = 45.00m,
    };

    private static object ARide() => new
    {
        title = "Lifts to appointments",
        description = "Happy to drive within the east end.",
        category = "Rides",
        neighbourhood = SeedData.Marion.Neighbourhood,
        windows = new[]
        {
            new { day = nameof(DayOfWeek.Tuesday), startsAt = "07:30", endsAt = "10:30" },
            new { day = nameof(DayOfWeek.Thursday), startsAt = "13:00", endsAt = "16:30" },
        },
    };

    private async Task<Guid> MarionPostsAsync(string kind, object body)
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync($"/listings/{kind}", body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<PostedListing>()).ListingId;
    }
}
