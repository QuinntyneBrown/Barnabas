using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Members;

// Acceptance Test
// Traces to: L2-020, L2-021, L2-022, L2-023, L2-024, L2-025, L2-069, L2-076, L2-077, L2-079
// Description: A member keeps their own profile, reads another's, and finds people through the
// directory. What the congregation sees of somebody never includes their email address, and
// nothing anywhere offers to message them directly.
public sealed class ProfilesAndDirectoryTests : AcceptanceTest
{
    public ProfilesAndDirectoryTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-020 AC1: Given a signed-in member, when they request their own profile, then their
    // display name, neighbourhood, description, and help tags are returned.
    [Fact]
    public async Task A_member_reads_their_own_profile()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await EditAsync(priya, description: "Two doors up from the church hall.", helpTags: ["Rides"]);

        var profile = await MyProfileAsync(priya);

        profile.DisplayName.ShouldBe(SeedData.Priya.DisplayName);
        profile.Neighbourhood.ShouldBe(SeedData.Priya.Neighbourhood);
        profile.Description.ShouldBe("Two doors up from the church hall.");
        profile.HelpTags.ShouldBe(["Rides"]);

        // Their own address, because this is the screen that says where their sign-in link goes.
        profile.EmailAddress.ShouldBe(SeedData.Priya.EmailAddress);

        // And what their congregation offers, so the form has something to choose from and it is
        // this congregation's list rather than a copy in the client.
        profile.Neighbourhoods.ShouldBe(SeedData.StAidans.Neighbourhoods);
        profile.AvailableHelpTags.ShouldNotBeEmpty();
    }

    // L2-021 AC1: Given a signed-in member, when they submit a changed display name, then the
    // change is persisted and returned on the next read.
    [Fact]
    public async Task A_changed_name_is_kept()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await EditAsync(priya, displayName: "Priya Kaur");

        (await MyProfileAsync(priya)).DisplayName.ShouldBe("Priya Kaur");
    }

    // L2-021 AC2: Given a signed-in member, when they submit a description longer than 1000
    // characters, then the response is 400 Bad Request and nothing is persisted.
    [Fact]
    public async Task An_over_long_description_is_refused_and_nothing_is_kept()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PutJsonAsync(
            "/members/me",
            AProfile(displayName: "Changed too", description: new string('a', Member.DescriptionMaxLength + 1)),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Description");

        // Nothing at all was written, not merely the description. The name in the same payload is
        // unchanged, because validation runs before the handler.
        (await MyProfileAsync(priya)).DisplayName.ShouldBe(SeedData.Priya.DisplayName);
    }

    // L2-022 AC1: Given a member of St. Aidan's, when they set their neighbourhood to a name
    // configured only in another congregation, then the response is 400 Bad Request.
    [Fact]
    public async Task A_neighbourhood_from_another_congregation_is_refused_naming_the_field()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        // Parkdale belongs to St. Brigid's, and to no list Priya may choose from.
        var response = await priya.PutJsonAsync(
            "/members/me",
            AProfile(neighbourhood: SeedData.StBrigids.Neighbourhoods[0]),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("neighbourhood");
    }

    // L2-023 AC1: Given a signed-in member, when they select two help tags, then both are
    // persisted and returned on their public profile.
    [Fact]
    public async Task Declared_help_tags_reach_the_public_profile()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await EditAsync(priya, helpTags: ["Rides", "Tutoring"]);

        var seenByMarion = await ProfileOfAsync(marion, SeedData.Priya.Id);

        seenByMarion.HelpTags.ShouldBe(["Rides", "Tutoring"]);
    }

    [Fact]
    public async Task A_help_tag_the_congregation_does_not_offer_is_refused()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PutJsonAsync(
            "/members/me",
            AProfile(helpTags: ["Dog walking"]),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("helpTags");
    }

    // L2-024 AC1: Given two approved members of one congregation, when one requests the other's
    // public profile, then name, neighbourhood, description, help tags, and active listings are
    // returned.
    [Fact]
    public async Task A_public_profile_carries_what_the_congregation_may_see()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await EditAsync(marion, description: "Happy to lend most things.", helpTags: ["Minor repairs"]);

        var profile = await ProfileOfAsync(priya, SeedData.Marion.Id);

        profile.DisplayName.ShouldBe(SeedData.Marion.DisplayName);
        profile.Neighbourhood.ShouldBe(SeedData.Marion.Neighbourhood);
        profile.Description.ShouldBe("Happy to lend most things.");
        profile.HelpTags.ShouldBe(["Minor repairs"]);

        // What she currently has on the board, so the profile is a way to what she is offering.
        profile.ActiveListings.ShouldContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // L2-024 AC2: Given any member's public profile, when it is returned, then it contains no
    // email address.
    [Fact]
    public async Task A_public_profile_carries_no_email_address()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var body = await (await priya.GetAsync(
                $"/members/{SeedData.Marion.Id}",
                TestContext.Current.CancellationToken))
            .ReadBodyAsync();

        body.ShouldNotContain(SeedData.Marion.EmailAddress);
        body.ShouldNotContain("@");
    }

    // L2-024: a member of another congregation is not found, and neither is one still waiting.
    // One answer for both, because distinguishing them would say who belongs where.
    [Fact]
    public async Task A_member_of_another_congregation_has_no_profile_here()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync(
            $"/members/{SeedData.Hank.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // L2-025 AC1: Given a member with two active listings, when they leave the congregation, then
    // neither listing appears on the board and their session is ended.
    [Fact]
    public async Task Leaving_takes_the_listings_off_the_board_and_ends_the_session()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            "/members/me/leave",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The session goes with them. A token outliving the membership would still be reading the
        // board.
        var afterwards = await marion.GetAsync("/board", TestContext.Current.CancellationToken);

        afterwards.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var board = await (await priya.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);
        board.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Drill);

        // Archived rather than closed out. Nothing was lent, sold or given away; she left.
        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Archived);
    }

    [Fact]
    public async Task A_member_who_has_left_cannot_sign_in_again()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync("/members/me/leave", new { }, TestContext.Current.CancellationToken);

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = SeedData.Marion.EmailAddress },
            TestContext.Current.CancellationToken);

        // Answered exactly as an unregistered address is, and nothing sent.
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Outbox.LatestFor(SeedData.Marion.EmailAddress).ShouldBeNull();
    }

    // L2-076 AC1: Given a congregation, when a member requests the directory, then members are
    // returned with name, neighbourhood, and help tags.
    [Fact]
    public async Task The_directory_lists_the_congregation()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await EditAsync(priya, helpTags: ["Rides"]);

        var directory = await DirectoryAsync(priya);

        directory.ShouldContain(member => member.MemberId == SeedData.Marion.Id);
        directory.ShouldContain(member => member.MemberId == SeedData.Grace.Id);

        var hers = directory.Single(member => member.MemberId == SeedData.Priya.Id);

        hers.DisplayName.ShouldBe(SeedData.Priya.DisplayName);
        hers.Neighbourhood.ShouldBe(SeedData.Priya.Neighbourhood);
        hers.HelpTags.ShouldBe(["Rides"]);
    }

    // L2-079 AC2: Given two congregations, when a member of one requests the directory, then no
    // member of the other appears.
    [Fact]
    public async Task The_directory_stops_at_the_congregation_boundary()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var directory = await DirectoryAsync(priya);

        directory.ShouldNotContain(member => member.MemberId == SeedData.Hank.Id);

        // And the administrator, who belongs to the platform congregation rather than this parish.
        directory.ShouldNotContain(member => member.MemberId == SeedData.Ada.Id);
    }

    // L2-079 AC1: Given a congregation containing a pending member, when the directory is
    // requested, then the pending member is absent.
    [Fact]
    public async Task A_member_still_waiting_is_not_in_the_directory()
    {
        var waiting = await AMemberAwaitingApprovalAsync();

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        (await DirectoryAsync(priya)).ShouldNotContain(member => member.MemberId == waiting);
    }

    // L2-079 AC3: Given the directory, when it is returned, then it contains no email addresses.
    [Fact]
    public async Task The_directory_carries_no_email_addresses()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var body = await (await priya.GetAsync("/directory", TestContext.Current.CancellationToken))
            .ReadBodyAsync();

        body.ShouldNotContain("@");
    }

    // L2-077 AC1: Given a directory, when it is searched by a member's name, then matching
    // members are returned.
    [Fact]
    public async Task The_directory_is_searchable_by_name()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var found = await DirectoryAsync(priya, term: "Marion");

        found.ShouldHaveSingleItem().MemberId.ShouldBe(SeedData.Marion.Id);
    }

    // L2-077 AC2: Given a directory, when it is searched by a help tag, then members carrying that
    // tag are returned.
    [Fact]
    public async Task The_directory_is_searchable_by_what_people_offer()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        await EditAsync(priya, helpTags: ["Rides"]);
        await EditAsync(grace, helpTags: ["Tutoring"], neighbourhood: SeedData.Grace.Neighbourhood, displayName: SeedData.Grace.DisplayName);

        var drivers = await DirectoryAsync(priya, helpTag: "Rides");

        drivers.ShouldHaveSingleItem().MemberId.ShouldBe(SeedData.Priya.Id);
    }

    // L2-069 AC1: Given two members with no accepted request between them, when one attempts to
    // create a thread with the other, then no such endpoint exists and the attempt returns 404.
    [Fact]
    public async Task There_is_no_way_to_message_a_member_from_their_profile()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        foreach (var route in new[]
        {
            $"/members/{SeedData.Marion.Id}/messages",
            $"/members/{SeedData.Marion.Id}/threads",
        })
        {
            var response = await priya.PostJsonAsync(
                route,
                new { body = "Hello" },
                TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        }
    }

    private static object AProfile(
        string? displayName = null,
        string? neighbourhood = null,
        string? description = null,
        string[]? helpTags = null) => new
    {
        displayName = displayName ?? SeedData.Priya.DisplayName,
        neighbourhood = neighbourhood ?? SeedData.Priya.Neighbourhood,
        description,
        helpTags = helpTags ?? [],
    };

    private static async Task EditAsync(
        HttpClient client,
        string? displayName = null,
        string? neighbourhood = null,
        string? description = null,
        string[]? helpTags = null)
    {
        var current = await MyProfileAsync(client);

        var response = await client.PutJsonAsync(
            "/members/me",
            new
            {
                displayName = displayName ?? current.DisplayName,
                neighbourhood = neighbourhood ?? current.Neighbourhood,
                description = description ?? current.Description,
                helpTags = helpTags ?? current.HelpTags.ToArray(),
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<MyProfileBody> MyProfileAsync(HttpClient client) =>
        await (await client.GetAsync("/members/me", TestContext.Current.CancellationToken))
            .ReadAsync<MyProfileBody>();

    private static async Task<MemberProfileBody> ProfileOfAsync(HttpClient client, Guid memberId) =>
        await (await client.GetAsync($"/members/{memberId}", TestContext.Current.CancellationToken))
            .ReadAsync<MemberProfileBody>();

    private static async Task<IReadOnlyList<DirectoryMemberBody>> DirectoryAsync(
        HttpClient client,
        string? term = null,
        string? helpTag = null)
    {
        var query = term is not null
            ? $"?term={Uri.EscapeDataString(term)}"
            : helpTag is not null
                ? $"?helpTag={Uri.EscapeDataString(helpTag)}"
                : string.Empty;

        return await (await client.GetAsync($"/directory{query}", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<DirectoryMemberBody>>();
    }

    /// <summary>Somebody who has joined and is waiting, arranged through the product.</summary>
    private async Task<Guid> AMemberAwaitingApprovalAsync()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await (await marion.PostJsonAsync("/invites", new { }, TestContext.Current.CancellationToken))
            .ReadAsync<IssuedInviteCode>();

        using var stranger = Api.CreateClient();

        var redeemed = await (await stranger.PostJsonAsync(
                "/invites/redeem",
                new { code = issued.Code },
                TestContext.Current.CancellationToken))
            .ReadAsync<RedeemedInviteCode>();

        var joined = await (await stranger.PostJsonAsync(
                "/joining/profile",
                new
                {
                    joiningToken = redeemed.JoiningToken,
                    emailAddress = "waiting@example.com",
                    displayName = "Waiting W.",
                    neighbourhood = SeedData.StAidans.Neighbourhoods[0],
                    reasonForJoining = (string?)null,
                },
                TestContext.Current.CancellationToken))
            .ReadAsync<JoinedCongregation>();

        return joined.MemberId;
    }

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .Where(listing => listing.Id == listingId)
            .Select(listing => listing.Status)
            .SingleAsync());
}
