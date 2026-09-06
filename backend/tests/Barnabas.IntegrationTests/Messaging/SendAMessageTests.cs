using System.Net;
using Barnabas.Domain.Messaging;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Messaging;

// Acceptance Test
// Traces to: L2-067
// Description: A party appends to a thread, and an empty or over-long message is refused with
// nothing appended.
public sealed class SendAMessageTests : AcceptanceTest
{
    public SendAMessageTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-067 AC1: Given a thread, when a party sends a message, then it is appended and returned
    // on the next read.
    [Fact]
    public async Task A_message_a_party_sends_appears_in_the_thread()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Could I collect it after the 10:30 service?" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, Api);

        var sent = await response.ReadAsync<MessageBody>();

        sent.SenderId.ShouldBe(SeedData.Priya.Id);
        sent.SentByCaller.ShouldBeTrue();

        var thread = await priya.OpenAsync(threadId);

        thread.Messages.ShouldHaveSingleItem().Body.ShouldBe("Could I collect it after the 10:30 service?");
    }

    // Both parties can append. A thread has two rightful actors, not an owner and a visitor.
    [Fact]
    public async Task Both_parties_can_append_to_the_thread()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await marion.SayAsync(threadId, "It is by the side door.");
        Api.Clock.Advance(TimeSpan.FromMinutes(1));
        await priya.SayAsync(threadId, "Collected, thank you.");

        (await priya.OpenAsync(threadId)).Messages.Count.ShouldBe(2);
    }

    // L2-067 AC2: Given a thread, when a party sends an empty message, then the response is 400
    // Bad Request and nothing is appended.
    [Fact]
    public async Task An_empty_message_is_refused_and_nothing_is_appended()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "   " },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Body");
        (await priya.OpenAsync(threadId)).Messages.ShouldBeEmpty();
    }

    // L2-067 AC3: Given a thread, when a party sends a message longer than 4000 characters, then
    // the response is 400 Bad Request.
    [Fact]
    public async Task An_over_long_message_is_refused_and_nothing_is_appended()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = new string('a', Message.BodyMaxLength + 1) },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Body");
        (await priya.OpenAsync(threadId)).Messages.ShouldBeEmpty();
    }
}
