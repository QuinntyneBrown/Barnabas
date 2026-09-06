using System.Net;
using Barnabas.Domain.Messaging;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Requests;

// Acceptance Test
// Traces to: L2-060, L2-061, L2-094
// Description: Accepting sets the request Accepted and opens exactly one thread; declining sets
// it Declined and opens none; a request is decided once; and only the listing's owner decides.
public sealed class AcceptAndDeclineTests : AcceptanceTest
{
    public AcceptAndDeclineTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-060 AC1: Given a pending request on a member's listing, when the owner accepts it, then
    // its status becomes Accepted and a message thread is created linking both members and the
    // listing.
    [Fact]
    public async Task Accepting_opens_the_thread_the_handoff_is_arranged_in()
    {
        var requestId = await AskToBorrowAsync();

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/requests/{requestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var accepted = await response.ReadAsync<AcceptedRequest>();

        (await StatusOfAsync(requestId)).ShouldBe(RequestStatus.Accepted);

        var thread = await Api.QueryAsync(context => context.Set<MessageThread>()
            .IgnoreQueryFilters()
            .SingleAsync(thread => thread.Id == accepted.ThreadId));

        thread.RequestId.ShouldBe(requestId);
        thread.ListingId.ShouldBe(SeedData.Listings.Ladder);
        thread.OwnerId.ShouldBe(SeedData.Marion.Id);
        thread.RequesterId.ShouldBe(SeedData.Priya.Id);
    }

    // L2-060 AC2: Given a request already accepted, when it is accepted again, then the response
    // is 409 Conflict and no second thread is created.
    [Fact]
    public async Task A_request_is_decided_once()
    {
        var requestId = await AskToBorrowAsync();

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var first = await marion.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken);
        var second = await marion.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountThreadsAsync()).ShouldBe(1);
    }

    // L2-061 AC4: Given a request already declined, when it is accepted, then the response is
    // 409 Conflict and no thread is created.
    [Fact]
    public async Task A_declined_request_cannot_then_be_accepted()
    {
        var requestId = await AskToBorrowAsync();

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync($"/requests/{requestId}/decline", new { }, TestContext.Current.CancellationToken);

        var accept = await marion.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken);

        accept.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountThreadsAsync()).ShouldBe(0);
    }

    // L2-061 AC1: Given a pending request, when the owner declines it, then its status becomes
    // Declined and no message thread exists for it.
    [Fact]
    public async Task Declining_records_the_decision_and_opens_no_thread()
    {
        var requestId = await AskToBorrowAsync();

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/requests/{requestId}/decline",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var declined = await response.ReadAsync<DeclinedRequest>();

        declined.Status.ShouldBe(nameof(RequestStatus.Declined));

        // A state change, not a deletion: the requester has to be able to discover what became
        // of their ask.
        (await StatusOfAsync(requestId)).ShouldBe(RequestStatus.Declined);
        (await CountThreadsAsync()).ShouldBe(0);
    }

    // L2-094 AC2: Given a request on another member's listing, when a third member attempts to
    // accept it, then the response is 403 Forbidden.
    [Fact]
    public async Task A_third_member_cannot_decide_somebody_elses_request()
    {
        var requestId = await AskToBorrowAsync();

        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        var accept = await grace.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken);
        var decline = await grace.PostJsonAsync($"/requests/{requestId}/decline", new { }, TestContext.Current.CancellationToken);

        accept.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        decline.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await StatusOfAsync(requestId)).ShouldBe(RequestStatus.Pending);
        (await CountThreadsAsync()).ShouldBe(0);
    }

    // The requester is not the decider either, even though the request is theirs.
    [Fact]
    public async Task The_requester_cannot_accept_their_own_request()
    {
        var requestId = await AskToBorrowAsync();

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await StatusOfAsync(requestId)).ShouldBe(RequestStatus.Pending);
    }

    // L2-060 AC4: Given a pending request accepted and declined concurrently, when both complete,
    // then exactly one transition succeeds, the other receives 409, and at most one thread exists.
    [RequiresPostgresFact]
    public async Task An_accept_racing_a_decline_produces_one_decision()
    {
        var requestId = await AskToBorrowAsync();

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var outcomes = await Task.WhenAll(
            marion.PostJsonAsync($"/requests/{requestId}/accept", new { }, TestContext.Current.CancellationToken),
            marion.PostJsonAsync($"/requests/{requestId}/decline", new { }, TestContext.Current.CancellationToken));

        // The status check inside the handler is a courtesy. What decides this is the row
        // version, and nothing else could.
        outcomes.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        outcomes.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (await CountThreadsAsync()).ShouldBeLessThanOrEqualTo(1);
    }

    private async Task<Guid> AskToBorrowAsync()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private Task<RequestStatus> StatusOfAsync(Guid requestId) =>
        Api.QueryAsync(async context =>
            (await context.Set<ListingRequest>().IgnoreQueryFilters().SingleAsync(r => r.Id == requestId)).Status);

    private Task<int> CountThreadsAsync() =>
        Api.QueryAsync(context => context.Set<MessageThread>().IgnoreQueryFilters().CountAsync());
}
