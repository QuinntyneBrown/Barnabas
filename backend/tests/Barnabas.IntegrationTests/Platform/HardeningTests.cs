using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Platform;

// Acceptance Test
// Traces to: L2-091, L2-092, L2-097, L2-098, L2-100, L2-101
// Description: Writes stop at the congregation boundary and so does a moderator's authority.
// Member-supplied text is stored and returned as the member typed it, executes nowhere, and is
// literal data to every query. Transport is HTTPS, access tokens are short-lived, and a refresh
// token is spent when it is used.
public sealed class HardeningTests : AcceptanceTest
{
    /// <summary>The classic, and still the clearest thing to type into a title.</summary>
    private const string Script = "<script>alert('xss')</script>";

    /// <summary>Quote, semicolon, comment marker and a drop. Every character a query might mind.</summary>
    private const string Injection = "'; DROP TABLE [Listings]; -- %_[a]";

    public HardeningTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-091 AC1: Given a listing in congregation B, when a member of A attempts to request it,
    // then the response is 404 Not Found and no request is created.
    [Fact]
    public async Task A_write_against_another_congregations_listing_is_not_found()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Canoe}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await CountRequestsAsync(SeedData.Listings.Canoe)).ShouldBe(0);
    }

    // L2-091 AC2: Given a thread in congregation B, when a member of A attempts to send a message
    // to it, then the response is 404 Not Found.
    [Fact]
    public async Task A_message_into_another_congregations_thread_is_not_found()
    {
        var threadId = await AThreadInStBrigidsAsync();

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body = "Is this still going?" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await CountMessagesAsync(threadId)).ShouldBe(0);
    }

    // L2-092 AC1: Given a moderator of congregation A, when they request the moderation queue of
    // B, then the response is 404 Not Found.
    [Fact]
    public async Task A_moderator_sees_only_their_own_congregations_queue()
    {
        // Something for Hank to have objected to in St. Brigid's, so there is a queue in B for a
        // moderator of A to fail to reach.
        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        await Api.ArrangeAsync(async context =>
        {
            var canoe = await context.Set<Listing>()
                .IgnoreQueryFilters()
                .SingleAsync(listing => listing.Id == SeedData.Listings.Canoe);

            canoe.Flag(Api.Clock.GetUtcNow());
        });

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var queue = await (await marion.GetAsync(
            "/moderation/listings",
            TestContext.Current.CancellationToken)).ReadAsync<IReadOnlyList<FlaggedListingBody>>();

        // There is no route that names a congregation, so B's queue is not addressable at all —
        // which is the strongest form of the requirement. What a moderator of A gets is A's queue,
        // and B's flagged canoe is simply absent from it.
        queue.ShouldNotContain(flagged => flagged.ListingId == SeedData.Listings.Canoe);

        // And reaching for the row directly answers not found rather than forbidden.
        var removed = await marion.PostJsonAsync(
            $"/moderation/listings/{SeedData.Listings.Canoe}/remove",
            new { },
            TestContext.Current.CancellationToken);

        removed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // L2-092 AC2: Given a moderator of A, when they approve a pending member of B, then the
    // response is 404 Not Found and the member remains pending.
    [Fact]
    public async Task A_moderator_cannot_approve_another_congregations_applicant()
    {
        var applicantId = Guid.NewGuid();

        await Api.ArrangeAsync(context =>
        {
            context.Add(Member.Join(
                applicantId,
                SeedData.StBrigids.Id,
                "waiting@example.com",
                "Waiting W.",
                "Parkdale",
                "New to Parkdale."));

            return Task.CompletedTask;
        });

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/moderation/members/{applicantId}/approve",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await StatusOfAsync(applicantId)).ShouldBe(MemberStatus.AwaitingApproval);
    }

    // L2-097 AC1: Given a listing description containing a script tag, when it is stored and
    // returned, then it is returned encoded and not as executable markup.
    [Fact]
    public async Task A_script_tag_comes_back_as_text_and_never_as_markup()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(marion, $"A ladder {Script}", $"Sound. {Script}");

        var response = await marion.GetAsync(
            $"/listings/{listingId}",
            TestContext.Current.CancellationToken);

        var raw = await response.ReadBodyAsync();

        // On the wire the angle brackets are escaped, so nothing that parses this document as
        // markup can find a script element in it.
        raw.ShouldNotContain("<script>");
        raw.ShouldContain("\\u003C");

        // And decoded it is exactly what the member typed. Escaping is a property of the
        // encoding, not a change to their words — L2-098 AC2 asks for the second half of that.
        var detail = await response.ReadAsync<ListingDetailBody>();

        detail.Title.ShouldBe($"A ladder {Script}");
        detail.Description.ShouldBe($"Sound. {Script}");
    }

    // L2-097 AC3: Given any page, when its headers are inspected, then a Content-Security-Policy
    // is present that forbids inline script.
    // [E2E] in the specification; asserted here as well, because the API is one of the two things
    // that serves a response and the header is emitted by its own middleware.
    [Fact]
    public async Task Every_response_carries_a_policy_that_forbids_inline_script()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        foreach (var route in new[] { "/board", "/health", "/nothing-is-here" })
        {
            var response = await marion.GetAsync(route, TestContext.Current.CancellationToken);

            response.Headers.TryGetValues("Content-Security-Policy", out var values)
                .ShouldBeTrue($"{route} carried no policy");

            var policy = values!.Single();

            policy.ShouldContain("script-src 'self'");

            // The half that matters. A policy naming script-src and then allowing inline would be
            // present and useless.
            policy.ShouldNotContain("'unsafe-inline'; ");
            policy.ShouldContain("object-src 'none'");
            policy.ShouldContain("frame-ancestors 'none'");
        }
    }

    // L2-098 AC1: Given a search term containing SQL control characters, when it is searched, then
    // it is treated as literal text and the query succeeds without error.
    [Fact]
    public async Task A_search_term_full_of_control_characters_is_just_a_term()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.GetAsync(
            $"/search?term={Uri.EscapeDataString(Injection)}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await response.ReadAsync<SearchPageBody>();

        page.Term.ShouldBe(Injection);
        page.Results.ShouldBeEmpty();

        // The table is still there, which is the whole point.
        (await CountListingsAsync()).ShouldBeGreaterThan(0);
    }

    // The wildcards in that term are literal too. A search for a percent sign matches a listing
    // containing one, not every listing there is.
    [Fact]
    public async Task Wildcards_in_a_term_match_themselves()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await PostAsync(marion, "100% wool blanket", "Warm, and heavier than it looks.");

        var everything = await SearchAsync(marion, "%");

        everything.Results.ShouldContain(result => result.Title == "100% wool blanket");

        // If the wildcard were passed through, this would return the ladder and the drill too.
        everything.Results.Count.ShouldBe(1);
    }

    // L2-098 AC2: Given a field containing SQL control characters, when it is stored and read back,
    // then the value is returned unchanged.
    [Fact]
    public async Task A_field_full_of_control_characters_comes_back_exactly_as_it_went_in()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(marion, $"Ladder {Injection}", $"Description {Injection}");

        var detail = await (await marion.GetAsync(
            $"/listings/{listingId}",
            TestContext.Current.CancellationToken)).ReadAsync<ListingDetailBody>();

        detail.Title.ShouldBe($"Ladder {Injection}");
        detail.Description.ShouldBe($"Description {Injection}");

        (await CountListingsAsync()).ShouldBeGreaterThan(0);
    }

    // L2-100 AC2: Given an issued access token, when it is inspected, then its lifetime is no
    // greater than 60 minutes.
    [Fact]
    public async Task An_access_token_is_short_lived()
    {
        var token = await Api.SignInAsync(SeedData.Marion.Id);

        var read = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var lifetime = read.ValidTo - read.ValidFrom;

        lifetime.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(60));
        lifetime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    // L2-100 AC1: Given a request over plain HTTP, when it is made, then it is redirected to HTTPS
    // and a Strict-Transport-Security header is returned.
    [Fact]
    public async Task Plain_http_is_redirected_and_the_answer_says_not_to_come_back_that_way()
    {
        // The suite drives the API over plain HTTP, so the redirect is off for every other test.
        // Turning it on for this one is what makes the requirement tested rather than assumed.
        using var https = Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:RequireHttps", "true");

            // The redirect middleware needs a port to redirect *to*. A test server binds no HTTPS
            // address, so without this it logs that it could not work one out and passes the
            // request through - the requirement would look met and would not be.
            builder.UseSetting("https_port", "443");
        });

        using var client = https.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.PermanentRedirect,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Redirect);

        response.Headers.Location!.Scheme.ShouldBe("https");

        // Emitted by our own middleware rather than by UseHsts, which skips any request that did
        // not arrive over HTTPS - behind a terminating proxy that is every request, so the
        // built-in one would quietly emit nothing in exactly the deployment this is meant for.
        response.Headers.TryGetValues("Strict-Transport-Security", out var hsts).ShouldBeTrue();
        hsts!.Single().ShouldContain("max-age=");
    }

    // L2-100 AC3: Given a refresh token, when it is used, then it is rotated and the previous value
    // is rejected on reuse.
    // Narrower than L2-018 AC3, which asserts the same behaviour from the session's side. It is
    // stated again here because L2-100 states it again, and a criterion nothing names is a
    // criterion nobody can show is met.
    [Fact]
    public async Task A_refresh_token_is_spent_when_it_is_used()
    {
        using var device = await Device.SignInAsync(Api, SeedData.Marion.EmailAddress);

        var first = device.RefreshToken;

        (await device.RenewAsync()).ShouldBe(HttpStatusCode.OK);

        // Rotated: the renewal handed back a different token.
        device.RefreshToken.ShouldNotBe(first);

        // And the old one is dead. The rotation is a conditional update whose affected-row count
        // is the answer, so two callers racing produce one winner rather than two valid tokens.
        (await device.RenewWithAsync(first)).ShouldBe(HttpStatusCode.Unauthorized);

        // The one that was handed back still works, so a member is not signed out by their own
        // renewal.
        (await device.RenewAsync()).ShouldBe(HttpStatusCode.OK);
    }

    // L2-101 AC1: Given any endpoint returning another member, when its response is inspected, then
    // it contains no email address.
    [Fact]
    public async Task No_endpoint_describing_another_member_carries_their_address()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        foreach (var route in new[]
                 {
                     "/board",
                     "/directory",
                     $"/members/{SeedData.Marion.Id}",
                     $"/listings/{SeedData.Listings.Ladder}",
                 })
        {
            var body = await (await priya.GetAsync(route, TestContext.Current.CancellationToken))
                .ReadBodyAsync();

            body.ShouldNotContain(SeedData.Marion.EmailAddress, Case.Insensitive, $"{route} disclosed it");
            body.ShouldNotContain("@example.com", Case.Insensitive, $"{route} disclosed an address");
        }

        // Their own is another matter: a member is entitled to see the address Barnabas has for
        // them, and the profile screen shows it.
        var mine = await (await priya.GetAsync("/members/me", TestContext.Current.CancellationToken))
            .ReadBodyAsync();

        mine.ShouldContain(SeedData.Priya.EmailAddress);
    }

    private static async Task<Guid> PostAsync(HttpClient client, string title, string description)
    {
        var response = await client.PostJsonAsync(
            "/listings/give",
            new
            {
                title,
                description,
                category = "Sundries",
                neighbourhood = SeedData.Marion.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<PostedListing>()).ListingId;
    }

    private static async Task<SearchPageBody> SearchAsync(HttpClient client, string term)
    {
        var response = await client.GetAsync(
            $"/search?term={Uri.EscapeDataString(term)}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<SearchPageBody>();
    }

    /// <summary>A thread wholly inside St. Brigid's, so a member of St. Aidan's has one to fail at.</summary>
    private async Task<Guid> AThreadInStBrigidsAsync()
    {
        var otherId = Guid.NewGuid();

        await Api.ArrangeAsync(context =>
        {
            context.Add(new Member(
                otherId,
                SeedData.StBrigids.Id,
                "other@example.com",
                "Other O.",
                "High Park",
                MemberRole.Member,
                MemberStatus.Approved));

            return Task.CompletedTask;
        });

        using var other = await Api.ClientForAsync(otherId);
        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        var made = await other.PostJsonAsync(
            $"/listings/{SeedData.Listings.Canoe}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        made.StatusCode.ShouldBe(HttpStatusCode.Created);

        var accepted = await hank.PostJsonAsync(
            $"/requests/{(await made.ReadAsync<MadeRequest>()).RequestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await accepted.ReadAsync<AcceptedRequest>()).ThreadId;
    }

    private Task<int> CountRequestsAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Domain.Requests.ListingRequest>()
            .IgnoreQueryFilters()
            .CountAsync(request => request.ListingId == listingId));

    private Task<int> CountMessagesAsync(Guid threadId) =>
        Api.QueryAsync(context => context
            .Set<Domain.Messaging.Message>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.ThreadId == threadId));

    private Task<int> CountListingsAsync() =>
        Api.QueryAsync(context => context.Set<Listing>().IgnoreQueryFilters().CountAsync());

    private Task<MemberStatus> StatusOfAsync(Guid memberId) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .Where(member => member.Id == memberId)
            .Select(member => member.Status)
            .SingleAsync());
}
