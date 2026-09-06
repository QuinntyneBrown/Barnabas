using System.Net;
using System.Text;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Platform;

// Acceptance Test
// Traces to: L2-096
// Description: Every field is bounded, a failure names the field without echoing what was sent,
// an unrecognised field is ignored, a forbidden one is refused, and an oversized body is refused
// before anything reads it.
public sealed class InputValidationTests : AcceptanceTest
{
    public InputValidationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-096 AC1: Given a field exceeding its documented maximum length, when it is submitted,
    // then the response is 400 Bad Request naming the field.
    [Fact]
    public async Task An_over_long_field_is_refused_naming_it()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostJsonAsync(
            "/listings/lend",
            new
            {
                title = new string('a', Listing.TitleMaxLength + 1),
                description = "Sound.",
                category = "Tools",
                neighbourhood = SeedData.Marion.Neighbourhood,
                returnBy = "2026-10-01",
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Title");
    }

    // L2-096 AC2: Given a request body exceeding 1 MB, when it is submitted, then the response
    // is 413 Payload Too Large.
    [Fact]
    public async Task A_body_over_a_megabyte_is_refused()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var oversized = new StringBuilder()
            .Append("{\"title\":\"A ladder\",\"description\":\"")
            .Append('a', (1024 * 1024) + 512)
            .Append("\",\"category\":\"Tools\",\"neighbourhood\":\"Riverdale\",\"returnBy\":\"2026-10-01\"}")
            .ToString();

        var response = await client.PostRawJsonAsync(
            "/listings/lend",
            oversized,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    // L2-096 AC3: Given a validation failure, when the error is returned, then it names the
    // field and does not echo the submitted value.
    [Fact]
    public async Task A_validation_failure_names_the_field_without_repeating_the_value()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        const string Submitted = "wildly-distinctive-value-nobody-else-would-write";

        var response = await client.PostJsonAsync(
            "/listings/lend",
            new
            {
                title = Submitted + new string('a', Listing.TitleMaxLength),
                description = "Sound.",
                category = "Tools",
                neighbourhood = SeedData.Marion.Neighbourhood,
                returnBy = "2026-10-01",
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.ReadBodyAsync();

        body.ShouldContain("Title");

        // Echoing is how a validation message becomes a reflection vector, and the member
        // already knows what they typed.
        body.ShouldNotContain(Submitted);
    }

    // L2-096 AC4: Given an unexpected field in a request body, when it is submitted, then it is
    // ignored rather than persisted.
    [Fact]
    public async Task An_unrecognised_field_is_ignored()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostRawJsonAsync(
            "/listings/lend",
            """
            {
              "title": "Extending ladder",
              "description": "Sound, both spreaders lock.",
              "category": "Tools",
              "neighbourhood": "Riverdale",
              "returnBy": "2026-10-01",
              "legacyTag": "carried by an older client"
            }
            """,
            TestContext.Current.CancellationToken);

        // A client sending a superseded field is not broken by a deployment.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.ReadAsync<PostedListing>();

        var stored = await Api.QueryAsync(context =>
            context.Set<Listing>().IgnoreQueryFilters().SingleAsync(l => l.Id == created.ListingId));

        stored.Title.ShouldBe("Extending ladder");
    }

    // L2-096 AC5: Given a request body carrying a field the command forbids for its kind, when
    // it is submitted, then the response is 400 Bad Request naming that field, rather than the
    // field being ignored.
    [Fact]
    public async Task A_forbidden_field_is_refused_by_name_rather_than_ignored()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await client.PostRawJsonAsync(
            "/listings/lend",
            """
            {
              "title": "Extending ladder",
              "description": "Sound, both spreaders lock.",
              "category": "Tools",
              "neighbourhood": "Riverdale",
              "returnBy": "2026-10-01",
              "price": 45.00
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("price");
    }
}
