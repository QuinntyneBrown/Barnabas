using System.Net;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Messaging;

// Acceptance Test
// Traces to: L2-066
// Description: A party opens a thread, sees its messages in the order they were said and
// attributed to who said them, and opening marks it read for that member alone.
public sealed class ReadAThreadTests : AcceptanceTest
{
    public ReadAThreadTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-066 AC1: Given a thread with four messages, when a party requests it, then all four are
    // returned in ascending time order with their sender.
    [Fact]
    public async Task A_thread_reads_in_the_order_things_were_said()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        // A minute apart, so the four have distinct times to be ordered by. Any longer and the
        // conversation would outlast the access tokens it is being held with.
        await marion.SayAsync(threadId, "Yes, it is still here. When suits you?");
        Api.Clock.Advance(TimeSpan.FromMinutes(1));
        await priya.SayAsync(threadId, "Thursday evening, after choir?");
        Api.Clock.Advance(TimeSpan.FromMinutes(1));
        await marion.SayAsync(threadId, "Thursday is fine. I will leave it by the side door.");
        Api.Clock.Advance(TimeSpan.FromMinutes(1));
        await priya.SayAsync(threadId, "Thank you, Marion.");

        var thread = await priya.OpenAsync(threadId);

        thread.Messages.Select(message => message.Body).ShouldBe([
            "Yes, it is still here. When suits you?",
            "Thursday evening, after choir?",
            "Thursday is fine. I will leave it by the side door.",
            "Thank you, Marion.",
        ]);

        thread.Messages.Select(message => message.SentAt).ShouldBeInOrder();

        var opening = thread.Messages[0];

        opening.SenderId.ShouldBe(SeedData.Marion.Id);
        opening.SenderDisplayName.ShouldBe(SeedData.Marion.DisplayName);

        // Read by Priya, so Marion is the other side of the conversation and Priya is not.
        opening.SentByCaller.ShouldBeFalse();
        thread.Messages[1].SentByCaller.ShouldBeTrue();
    }

    // A thread always carries the listing it concerns and the standing of the request that
    // opened it. There is no such thing here as a conversation about nothing.
    [Fact]
    public async Task A_thread_carries_its_listing_and_the_decision_that_opened_it()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var thread = await priya.OpenAsync(threadId);

        thread.ListingId.ShouldBe(SeedData.Listings.Ladder);
        thread.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
        thread.OtherMemberId.ShouldBe(SeedData.Marion.Id);
        thread.OtherMemberDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        thread.RequestStatus.ShouldBe(nameof(RequestStatus.Accepted));
    }

    // L2-066 AC2: Given an unread thread, when a party opens it, then its unread flag becomes
    // false for that member only.
    [Fact]
    public async Task Opening_a_thread_marks_it_read_for_the_reader_alone()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await marion.SayAsync(threadId, "Yes, it is still here.");

        (await UnreadForAsync(priya)).ShouldBeTrue();

        await priya.OpenAsync(threadId);

        (await UnreadForAsync(priya)).ShouldBeFalse();

        Api.Clock.Advance(TimeSpan.FromMinutes(1));
        await priya.SayAsync(threadId, "Thursday evening?");

        // Marion has not read that, and nothing Priya did changes it. Unread is a property of
        // the reader, so the two can legitimately disagree about the same thread.
        (await UnreadForAsync(marion)).ShouldBeTrue();
        (await UnreadForAsync(priya)).ShouldBeFalse();
    }

    // L2-066 AC3: Given a thread, when a member who is not a party requests it, then the
    // response is 404 Not Found.
    [Fact]
    public async Task A_member_who_is_not_a_party_is_told_the_thread_does_not_exist()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        var read = await grace.GetAsync($"/threads/{threadId}", TestContext.Current.CancellationToken);

        // Not forbidden. A thread's existence is not disclosed to somebody outside it, which is
        // the same reasoning that governs cross-congregation access.
        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var send = await grace.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Is that ladder going spare?" },
            TestContext.Current.CancellationToken);

        send.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<bool> UnreadForAsync(HttpClient client) =>
        (await client.ThreadsAsync()).Single().Unread;
}
