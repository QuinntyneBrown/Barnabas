using System.Net;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Requests;

// Acceptance Test
// Traces to: L2-054, L2-062
// Description: A member asks to borrow a Lend listing, and each of the four conditions a request
// has to satisfy is enforced.
public sealed class MakeARequestTests : AcceptanceTest
{
    public MakeARequestTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    private static string Loan(Guid listingId) => $"/listings/{listingId}/requests/loan";

    // L2-054 AC1: Given an active Lend listing, when a member submits a request with a message,
    // pickup date, return date, and acknowledgement, then a request is created with status
    // Pending.
    [Fact]
    public async Task A_request_to_borrow_is_created_pending()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var made = await response.ReadAsync<MadeRequest>();

        var stored = await Api.QueryAsync(context => context.Set<ListingRequest>()
            .IgnoreQueryFilters()
            .SingleAsync(request => request.Id == made.RequestId));

        stored.Status.ShouldBe(RequestStatus.Pending);
        stored.RequesterId.ShouldBe(SeedData.Priya.Id);
        stored.ListingId.ShouldBe(SeedData.Listings.Ladder);
        stored.LoanTerms.ShouldNotBeNull();
        stored.LoanTerms.ReturnBy.ShouldBe(new DateOnly(2026, 9, 20));
    }

    // L2-054 AC2: Given a Lend request submitted without a return date, when it is posted, then
    // the response is 400 Bad Request naming the field.
    [Fact]
    public async Task A_request_without_a_return_date_is_refused_naming_it()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostRawJsonAsync(
            Loan(SeedData.Listings.Ladder),
            """
            { "message": "Painting the back bedroom.", "pickupOn": "2026-09-13", "loanAcknowledged": true }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("ReturnBy");
    }

    // L2-054: the acknowledgement is required rather than decorative. A loan the requester has
    // not acknowledged as a loan is the misunderstanding the field exists to prevent.
    [Fact]
    public async Task A_request_without_the_acknowledgement_is_refused()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(acknowledged: false),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("LoanAcknowledged");
    }

    // L2-062 AC1: Given a member's own listing, when they request it, then the response is 400
    // Bad Request and no request is created.
    [Fact]
    public async Task A_member_cannot_request_their_own_listing()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CountRequestsAsync()).ShouldBe(0);
    }

    // L2-062 AC2: Given a member with a pending request on a listing, when they request the same
    // listing again, then the response is 409 Conflict.
    [Fact]
    public async Task A_member_holds_at_most_one_open_request_against_a_listing()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var first = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        var second = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountRequestsAsync()).ShouldBe(1);
    }

    // L2-062 AC3: Given a member whose request on a listing was declined, when they request that
    // listing again, then a new request is created.
    [Fact]
    public async Task A_declined_request_does_not_block_asking_again()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var first = await (await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken)).ReadAsync<MadeRequest>();

        await marion.PostJsonAsync(
            $"/requests/{first.RequestId}/decline",
            new { },
            TestContext.Current.CancellationToken);

        var again = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        // The rule is about open requests. A declined one is settled and the member is free to
        // ask again, which is exactly why the index that enforces it is filtered.
        again.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await CountRequestsAsync()).ShouldBe(2);
    }

    // L2-062 AC5: Given two identical requests on one listing submitted concurrently by the same
    // member, when both complete, then exactly one is created and the other receives 409.
    [RequiresPostgresFact]
    public async Task Two_identical_requests_at_once_produce_one_request()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var responses = await Task.WhenAll(
            priya.PostJsonAsync(Loan(SeedData.Listings.Ladder), Fixtures.Requests.ToBorrow(), TestContext.Current.CancellationToken),
            priya.PostJsonAsync(Loan(SeedData.Listings.Ladder), Fixtures.Requests.ToBorrow(), TestContext.Current.CancellationToken));

        // The policy check gives a clean answer in the ordinary case; under a race only the
        // filtered unique index can decide, and this is the test that says so.
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (await CountRequestsAsync()).ShouldBe(1);
    }

    // L2-062 AC6: Given a Sell listing, when a member submits a request to the loan endpoint
    // naming it, then the response is 400 Bad Request and no request is created.
    [Fact]
    public async Task A_sell_listing_cannot_acquire_a_loan_request()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Drill),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CountRequestsAsync()).ShouldBe(0);
    }

    // L2-062 AC7: Given a listing that has been closed out or archived, when a member requests
    // it, then the response is 409 Conflict and no request is created.
    [Fact]
    public async Task A_closed_out_listing_cannot_be_requested()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        var response = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Ladder),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountRequestsAsync()).ShouldBe(0);
    }

    // A listing in another congregation is not visible, so it cannot be requested either - and
    // the answer says nothing about whether it exists.
    [Fact]
    public async Task A_listing_in_another_congregation_cannot_be_requested()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            Loan(SeedData.Listings.Canoe),
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CountRequestsAsync()).ShouldBe(0);
    }

    private Task<int> CountRequestsAsync() =>
        Api.QueryAsync(context => context.Set<ListingRequest>().IgnoreQueryFilters().CountAsync());
}
