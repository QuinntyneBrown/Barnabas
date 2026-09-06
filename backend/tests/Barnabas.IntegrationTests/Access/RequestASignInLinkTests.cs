using System.Net;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Access;

// Acceptance Test
// Traces to: L2-012
// Description: A member asks for a sign-in link and one is dispatched, without a password
// anywhere in the system and without a congregation in scope.
public sealed class RequestASignInLinkTests : AcceptanceTest
{
    public RequestASignInLinkTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-012 AC1: Given an approved member's email address, when a sign-in link is requested,
    // then the response is 202 Accepted and a link is dispatched to that address.
    [Fact]
    public async Task An_approved_member_is_sent_a_link()
    {
        using var client = Api.CreateClient();

        var response = await client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = SeedData.Priya.EmailAddress },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Outbox.LatestFor(SeedData.Priya.EmailAddress).ShouldNotBeNull();
    }

    // L2-012 AC2: Given any member record, when it is inspected, then it holds no password or
    // password hash field.
    [Fact]
    public void A_member_record_holds_no_password()
    {
        var suspect = typeof(Member)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => name.Contains("password", StringComparison.OrdinalIgnoreCase)
                || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || name.Contains("hash", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        suspect.ShouldBeEmpty();
    }

    // L2-012 AC4: Given an anonymous caller, when a sign-in link is requested, then the member
    // lookup succeeds without a congregation context, and no listing, request, or thread is
    // readable on that request.
    [Fact]
    public async Task An_anonymous_caller_can_be_found_but_can_read_nothing()
    {
        using var client = Api.CreateClient();

        var lookup = await client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = SeedData.Marion.EmailAddress },
            TestContext.Current.CancellationToken);

        lookup.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Outbox.LatestFor(SeedData.Marion.EmailAddress).ShouldNotBeNull();

        foreach (var route in new[] { "/board", "/requests/incoming", "/requests/mine", "/threads" })
        {
            var read = await client.GetAsync(route, TestContext.Current.CancellationToken);

            read.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{route} was readable anonymously");
        }
    }

    // The address is not echoed back, and an unregistered one is answered the same way. L2-013
    // is a later slice; this asserts only that the shape it needs is not being designed out.
    [Fact]
    public async Task An_unregistered_address_is_answered_the_same_way()
    {
        using var client = Api.CreateClient();

        var response = await client.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = "nobody@example.com" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Outbox.LatestFor("nobody@example.com").ShouldBeNull();
    }
}
