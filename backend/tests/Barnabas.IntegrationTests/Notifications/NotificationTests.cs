using System.Net;
using Barnabas.Domain.Notifications;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Notifications;

// Acceptance Test
// Traces to: L2-070, L2-071, L2-072, L2-073, L2-074, L2-075
// Description: A member is told when somebody asks for something of theirs, when a request of
// theirs is decided, and when somebody writes to them - and about nothing they have said they do
// not want to hear about.
public sealed class NotificationTests : AcceptanceTest
{
    public NotificationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-070 AC1: Given a member's active listing, when another member requests it, then a
    // notification is created for the owner naming the requester and the listing.
    [Fact]
    public async Task An_owner_is_told_who_asked_and_for_what()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await AskToBorrowAsync(priya);

        var hers = await NotificationsOfAsync(marion);

        var told = hers.ShouldHaveSingleItem();

        told.Kind.ShouldBe(nameof(NotificationKind.RequestReceived));
        told.SubjectMemberDisplayName.ShouldBe(SeedData.Priya.DisplayName);
        told.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);

        // L2-074: it leads somewhere. Every kind carries the identifiers its destination needs.
        told.RequestId.ShouldNotBeNull();
        told.ListingId.ShouldBe(SeedData.Listings.Ladder);

        // And the requester is told nothing: they know what they just did.
        (await NotificationsOfAsync(priya)).ShouldBeEmpty();
    }

    // L2-071 AC1: Given a pending request, when the owner accepts it, then a notification is
    // created for the requester stating it was accepted.
    [Fact]
    public async Task A_requester_is_told_when_it_is_accepted_and_where_to_go()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var requestId = await AskToBorrowAsync(priya);

        await DecideAsync(marion, requestId, "accept");

        var told = (await NotificationsOfAsync(priya)).ShouldHaveSingleItem();

        told.Kind.ShouldBe(nameof(NotificationKind.RequestAccepted));
        told.SubjectMemberDisplayName.ShouldBe(SeedData.Marion.DisplayName);

        // An acceptance opens a thread, and the notification carries it - which is the difference
        // between being told and being able to act on it.
        told.ThreadId.ShouldNotBeNull();
    }

    // L2-071 AC2: Given a pending request, when the owner declines it, then a notification is
    // created for the requester stating it was declined.
    [Fact]
    public async Task A_requester_is_told_when_it_is_declined()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var requestId = await AskToBorrowAsync(priya);

        await DecideAsync(marion, requestId, "decline");

        var told = (await NotificationsOfAsync(priya)).ShouldHaveSingleItem();

        told.Kind.ShouldBe(nameof(NotificationKind.RequestDeclined));

        // No thread, because a decline opens none. It still leads to the listing and the request.
        told.ThreadId.ShouldBeNull();
        told.ListingId.ShouldBe(SeedData.Listings.Ladder);
    }

    // L2-072 AC1: Given a thread, when one party sends a message, then a notification is created
    // for the other party and not for the sender.
    [Fact]
    public async Task Only_the_other_party_is_told_about_a_message()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var requestId = await AskToBorrowAsync(priya);
        var threadId = await DecideAsync(marion, requestId, "accept");

        await MarkAllReadAsync(priya);
        await MarkAllReadAsync(marion);

        var sent = await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Would Saturday morning suit?" },
            TestContext.Current.CancellationToken);

        sent.StatusCode.ShouldBe(HttpStatusCode.Created);

        var hers = await UnreadOfAsync(marion);
        var hersOwn = await UnreadOfAsync(priya);

        hers.Unread.ShouldBe(1);

        // The sender hears nothing about their own message.
        hersOwn.Unread.ShouldBe(0);

        (await NotificationsOfAsync(marion)).First().Kind
            .ShouldBe(nameof(NotificationKind.MessageReceived));
    }

    // L2-073 AC1: Given a member with unread notifications, when they request the count, then it
    // is returned.
    // L2-073 AC3: Given a member who marks all read, then the count is zero.
    [Fact]
    public async Task The_unread_count_is_what_is_unread_and_clears_when_read()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await AskToBorrowAsync(priya);
        await AskToBuyTheDrillAsync(grace);

        (await UnreadOfAsync(marion)).Unread.ShouldBe(2);

        await MarkAllReadAsync(marion);

        (await UnreadOfAsync(marion)).Unread.ShouldBe(0);

        // Read, not deleted. A member looking back should still find what they were told.
        (await NotificationsOfAsync(marion)).Count.ShouldBe(2);
        (await NotificationsOfAsync(marion)).ShouldAllBe(notification => !notification.Unread);
    }

    [Fact]
    public async Task One_notification_can_be_marked_read_on_its_own()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await AskToBorrowAsync(priya);
        await AskToBuyTheDrillAsync(grace);

        var first = (await NotificationsOfAsync(marion)).First();

        var read = await marion.PostJsonAsync(
            $"/notifications/read?notificationId={first.NotificationId}",
            new { },
            TestContext.Current.CancellationToken);

        read.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await UnreadOfAsync(marion)).Unread.ShouldBe(1);
    }

    // L2-074 AC1: Given a member's notification list, when each is inspected, then every one leads
    // to the listing, request, or thread it concerns.
    [Fact]
    public async Task Every_notification_leads_somewhere()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var requestId = await AskToBorrowAsync(priya);
        var threadId = await DecideAsync(marion, requestId, "accept");

        await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Thank you." },
            TestContext.Current.CancellationToken);

        foreach (var notification in await NotificationsOfAsync(marion))
        {
            // A notification without a destination cannot be constructed, so this is checking a
            // property of the factories rather than of any particular row.
            var leadsSomewhere =
                notification.ListingId is not null
                || notification.RequestId is not null
                || notification.ThreadId is not null;

            leadsSomewhere.ShouldBeTrue($"{notification.Kind} led nowhere");
        }
    }

    // L2-075 AC1: Given a member who has disabled new-message notifications, when a message is
    // sent to them, then no notification of that kind is created for them.
    // L2-075 AC2: and a request notification is still created.
    [Fact]
    public async Task A_disabled_kind_is_never_created_and_the_others_carry_on()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var silenced = await marion.PutJsonAsync(
            "/notifications/preferences",
            new
            {
                preferences = new[]
                {
                    new { kind = nameof(NotificationKind.MessageReceived), enabled = false },
                },
            },
            TestContext.Current.CancellationToken);

        silenced.StatusCode.ShouldBe(HttpStatusCode.OK);

        var requestId = await AskToBorrowAsync(priya);
        var threadId = await DecideAsync(marion, requestId, "accept");

        await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Would Saturday morning suit?" },
            TestContext.Current.CancellationToken);

        var hers = await NotificationsOfAsync(marion);

        // Never created, not merely hidden: the row is absent from the database, which is what
        // AC1 asks for and what filtering at the read could not deliver.
        hers.ShouldNotContain(notification => notification.Kind == nameof(NotificationKind.MessageReceived));
        (await CountOfKindAsync(SeedData.Marion.Id, NotificationKind.MessageReceived)).ShouldBe(0);

        // The request notification is untouched. Silencing one kind does not silence another.
        hers.ShouldContain(notification => notification.Kind == nameof(NotificationKind.RequestReceived));
    }

    [Fact]
    public async Task Preferences_come_back_as_they_were_set()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var before = await PreferencesOfAsync(marion);

        // Every configurable kind, enabled, without a row existing for any of them.
        before.ShouldAllBe(preference => preference.Enabled);
        before.Count.ShouldBe(NotificationPreferences.Configurable.Count);

        await marion.PutJsonAsync(
            "/notifications/preferences",
            new
            {
                preferences = new[]
                {
                    new { kind = nameof(NotificationKind.RequestReceived), enabled = false },
                },
            },
            TestContext.Current.CancellationToken);

        var after = await PreferencesOfAsync(marion);

        after.Single(preference => preference.Kind == nameof(NotificationKind.RequestReceived))
            .Enabled.ShouldBeFalse();

        after.Single(preference => preference.Kind == nameof(NotificationKind.MessageReceived))
            .Enabled.ShouldBeTrue();
    }

    // A moderator's removal is not among the kinds a member may switch off. L2-084 requires the
    // owner to be told, and a decision they could opt out of hearing would be made behind their
    // back.
    [Fact]
    public async Task A_moderators_removal_cannot_be_silenced()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PutJsonAsync(
            "/notifications/preferences",
            new
            {
                preferences = new[]
                {
                    new { kind = nameof(NotificationKind.ListingRemoved), enabled = false },
                },
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Accepted and ignored: the kind is not offered, so nothing was stored for it.
        (await PreferencesOfAsync(marion))
            .ShouldNotContain(preference => preference.Kind == nameof(NotificationKind.ListingRemoved));
    }

    private static async Task<Guid> AskToBorrowAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private static async Task<Guid> AskToBuyTheDrillAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Drill}/requests/purchase",
            new { message = "Is it still going?", pickupAt = "2026-10-01T14:00:00+00:00" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private static async Task<Guid?> DecideAsync(HttpClient client, Guid requestId, string decision)
    {
        var response = await client.PostJsonAsync(
            $"/requests/{requestId}/{decision}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return decision == "accept" ? (await response.ReadAsync<AcceptedRequest>()).ThreadId : null;
    }

    private static async Task<IReadOnlyList<NotificationBody>> NotificationsOfAsync(HttpClient client) =>
        await (await client.GetAsync("/notifications", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<NotificationBody>>();

    private static async Task<UnreadCountBody> UnreadOfAsync(HttpClient client) =>
        await (await client.GetAsync("/notifications/unread-count", TestContext.Current.CancellationToken))
            .ReadAsync<UnreadCountBody>();

    private static async Task<IReadOnlyList<NotificationPreferenceBody>> PreferencesOfAsync(
        HttpClient client) =>
        await (await client.GetAsync("/notifications/preferences", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<NotificationPreferenceBody>>();

    private static async Task MarkAllReadAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            "/notifications/read",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private Task<int> CountOfKindAsync(Guid recipientId, NotificationKind kind) =>
        Api.QueryAsync(context => context
            .Set<Notification>()
            .IgnoreQueryFilters()
            .CountAsync(notification => notification.RecipientId == recipientId && notification.Kind == kind));
}
