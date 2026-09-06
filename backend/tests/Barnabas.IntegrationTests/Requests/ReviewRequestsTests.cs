using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Requests;

// Acceptance Test
// Traces to: L2-059, L2-120
// Description: An owner sees the requests made on their listings, a requester sees the requests
// they made, and neither sees anybody else's.
public sealed class ReviewRequestsTests : AcceptanceTest
{
    public ReviewRequestsTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-059 AC1: Given a member with requests on their listings, when they request their
    // incoming requests, then each is returned with requester, listing, message, and status.
    [Fact]
    public async Task An_owner_sees_what_was_asked_of_them()
    {
        await AskToBorrowAsync(SeedData.Priya.Id, "Painting the back bedroom the weekend after next.");
        await AskToBorrowAsync(SeedData.Grace.Id, "Clearing the gutters before the rain.");

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.GetAsync("/requests/incoming", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var incoming = await response.ReadAsync<IReadOnlyList<IncomingRequestBody>>();

        incoming.Count.ShouldBe(2);

        var fromPriya = incoming.Single(request => request.RequesterId == SeedData.Priya.Id);

        fromPriya.RequesterDisplayName.ShouldBe(SeedData.Priya.DisplayName);
        fromPriya.ListingId.ShouldBe(SeedData.Listings.Ladder);
        fromPriya.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
        fromPriya.Kind.ShouldBe(nameof(ListingKind.Lend));
        fromPriya.Message.ShouldBe("Painting the back bedroom the weekend after next.");
        fromPriya.Status.ShouldBe(nameof(RequestStatus.Pending));

        // The terms belonging to the listing's kind. For a loan that is when it comes back,
        // which is the whole basis for the decision.
        fromPriya.Terms.ShouldContain("2026-09-20");
    }

    // L2-059 AC3: Given a member, when they request incoming requests, then requests on other
    // members' listings are absent.
    [Fact]
    public async Task An_owner_sees_nothing_asked_of_anybody_else()
    {
        await AskToBorrowAsync(SeedData.Priya.Id);

        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        var incoming = await (await grace.GetAsync("/requests/incoming", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<IncomingRequestBody>>();

        incoming.ShouldBeEmpty();
    }

    // L2-120 AC1: Given a member with requests, when they request their own requests, then each
    // is returned with the listing, its owner, the terms, and the status.
    [Fact]
    public async Task A_requester_sees_what_they_asked_for()
    {
        await AskToBorrowAsync(SeedData.Priya.Id);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var mine = await response.ReadAsync<IReadOnlyList<MyRequestBody>>();

        var only = mine.ShouldHaveSingleItem();

        only.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
        only.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        only.Neighbourhood.ShouldBe(SeedData.Marion.Neighbourhood);
        only.Status.ShouldBe(nameof(RequestStatus.Pending));
        only.Terms.ShouldNotBeNullOrWhiteSpace();

        // Nothing to open yet: a pending request has no thread, and the screen decides what to
        // offer from the data rather than by asking again.
        only.ThreadId.ShouldBeNull();
    }

    // L2-120 AC2: Given a member, when they request their own requests, then requests made by
    // other members are absent.
    [Fact]
    public async Task A_requester_sees_nothing_anybody_else_asked_for()
    {
        await AskToBorrowAsync(SeedData.Grace.Id);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var mine = await (await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        mine.ShouldBeEmpty();
    }

    // L2-120 AC3: Given an accepted request, when the member's own requests are returned, then
    // the row carries the identifier of the thread it opened.
    [Fact]
    public async Task An_accepted_request_carries_the_thread_it_opened()
    {
        var requestId = await AskToBorrowAsync(SeedData.Priya.Id);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var accepted = await (await marion.PostJsonAsync(
            $"/requests/{requestId}/accept",
            new { },
            TestContext.Current.CancellationToken)).ReadAsync<AcceptedRequest>();

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var mine = await (await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        var only = mine.ShouldHaveSingleItem();

        only.Status.ShouldBe(nameof(RequestStatus.Accepted));
        only.ThreadId.ShouldBe(accepted.ThreadId);
    }

    // L2-120 AC5 in its API half: a declined request is shown as declined and offers no thread,
    // because declining opens none.
    [Fact]
    public async Task A_declined_request_offers_no_thread()
    {
        var requestId = await AskToBorrowAsync(SeedData.Priya.Id);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync($"/requests/{requestId}/decline", new { }, TestContext.Current.CancellationToken);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var mine = await (await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        var only = mine.ShouldHaveSingleItem();

        only.Status.ShouldBe(nameof(RequestStatus.Declined));
        only.ThreadId.ShouldBeNull();
    }

    private async Task<Guid> AskToBorrowAsync(Guid requesterId, string? message = null)
    {
        using var client = await Api.ClientForAsync(requesterId);

        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(message),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }
}
