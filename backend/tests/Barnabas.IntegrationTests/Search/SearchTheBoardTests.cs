using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Search;

// Acceptance Test
// Traces to: L2-048, L2-049, L2-050, L2-051, L2-053, L2-088, L2-098
// Description: A member finds something on their own congregation's board by what it is called,
// narrows by kind, neighbourhood or price, and never sees another parish's listing or one that
// has already gone.
public sealed class SearchTheBoardTests : AcceptanceTest
{
    public SearchTheBoardTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-048 AC1: Given a listing titled `6ft aluminum step ladder`, when a member searches for
    // `ladder`, then that listing is among the results.
    [Fact]
    public async Task A_term_finds_the_listing_it_names()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var results = await SearchAsync(priya, "ladder");

        results.Results.ShouldContain(result => result.ListingId == SeedData.Listings.Ladder);
        results.Term.ShouldBe("ladder");
    }

    // The description is searched too, so a member who remembers what something was described as
    // rather than what it was called still finds it.
    [Fact]
    public async Task A_term_in_the_description_finds_it_as_well()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var results = await SearchAsync(priya, "spreaders");

        results.Results.ShouldContain(result => result.ListingId == SeedData.Listings.Ladder);
    }

    // L2-048 AC2: Given a search term matching nothing, when it is searched, then an empty
    // collection is returned with a 200 status.
    [Fact]
    public async Task A_term_matching_nothing_is_an_empty_answer_and_not_a_failure()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync(
            "/search?term=harpsichord",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await response.ReadAsync<SearchPageBody>();

        page.Results.ShouldBeEmpty();

        // The term comes back, so a screen with nothing on it can still say what was searched.
        page.Term.ShouldBe("harpsichord");
    }

    // L2-048 AC3: Given a search term of `LADDER`, when it is searched, then matching is
    // case-insensitive and the same results are returned as for `ladder`.
    //
    // A property of the column's collation, declared in the model, rather than of whichever
    // collation this machine's SQL Server happened to be installed with.
    [Fact]
    public async Task Searching_is_case_insensitive()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var shouted = await SearchAsync(priya, "LADDER");
        var spoken = await SearchAsync(priya, "ladder");

        shouted.Results.Select(result => result.ListingId)
            .ShouldBe(spoken.Results.Select(result => result.ListingId));

        shouted.Results.ShouldNotBeEmpty();
    }

    // L2-049 AC1: Given results spanning several kinds, when the search is narrowed to Sell, then
    // only Sell listings are returned.
    [Fact]
    public async Task Results_narrow_to_one_kind()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await PostAsync(marion, "sell", ASellListing("Folding ladder, aluminium", 60m));

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var everything = await SearchAsync(priya, "ladder");
        var forSale = await SearchAsync(priya, "ladder", kind: nameof(ListingKind.Sell));

        everything.Results.Count.ShouldBeGreaterThan(forSale.Results.Count);
        forSale.Results.ShouldAllBe(result => result.Kind == nameof(ListingKind.Sell));
        forSale.Results.ShouldNotBeEmpty();
    }

    // L2-050 AC1: Given results across several neighbourhoods, when the search is narrowed to
    // `Riverdale`, then only listings in that neighbourhood are returned.
    [Fact]
    public async Task Results_narrow_to_one_neighbourhood()
    {
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        // Grace is in The Danforth, so this listing is somewhere Marion's is not.
        await PostAsync(grace, "lend", new
        {
            title = "Extending ladder",
            description = "Sound.",
            category = "Tools",
            neighbourhood = SeedData.Grace.Neighbourhood,
            returnBy = "2026-12-01",
        });

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var inRiverdale = await SearchAsync(priya, "ladder", neighbourhood: SeedData.Marion.Neighbourhood);

        inRiverdale.Results.ShouldNotBeEmpty();
        inRiverdale.Results.ShouldAllBe(result => result.Neighbourhood == SeedData.Marion.Neighbourhood);
    }

    // L2-051 AC1: Given Sell listings priced 35, 45, and 60, when the search is narrowed to a
    // maximum of 50, then only the 35 and 45 listings are returned.
    [Fact]
    public async Task Results_narrow_to_a_price_ceiling()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await PostAsync(marion, "sell", ASellListing("Ladder, wooden", 35m));
        await PostAsync(marion, "sell", ASellListing("Ladder, extending", 60m));

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var affordable = await SearchAsync(priya, "ladder", maxPrice: 50m);

        affordable.Results.ShouldNotBeEmpty();
        affordable.Results.ShouldAllBe(result => result.Price <= 50m);
        affordable.Results.ShouldNotContain(result => result.Title == "Ladder, extending");
    }

    // L2-051 AC2: Given a price filter applied to a search, when results are returned, then Lend,
    // Give, and Help listings are excluded, because they carry no price.
    //
    // Somebody filtering by price is shopping. A free ladder is not an answer to that, and the
    // null test says so without naming a kind.
    [Fact]
    public async Task A_price_filter_excludes_everything_that_has_no_price()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var priced = await SearchAsync(priya, "ladder", maxPrice: 1000m);

        priced.Results.ShouldNotContain(result => result.ListingId == SeedData.Listings.Ladder);
        priced.Results.ShouldAllBe(result => result.Kind == nameof(ListingKind.Sell));
    }

    // L2-053 AC1: Given a listing marked sold whose title matches a term, when that term is
    // searched, then the listing is absent from the results.
    [Fact]
    public async Task A_listing_that_has_gone_is_not_found()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var closed = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Drill}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        closed.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        (await SearchAsync(priya, "drill")).Results
            .ShouldNotContain(result => result.ListingId == SeedData.Listings.Drill);
    }

    // L2-053 AC2: an archived listing is absent too.
    [Fact]
    public async Task A_listing_taken_off_the_board_is_not_found()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/archive",
            new { },
            TestContext.Current.CancellationToken);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        (await SearchAsync(priya, "ladder")).Results
            .ShouldNotContain(result => result.ListingId == SeedData.Listings.Ladder);
    }

    // L2-088 AC2: Given congregations A and B, when a member of A searches for a term matching a
    // listing in B, then that listing is absent from the results.
    //
    // The last outstanding criterion of L2-088. There is no congregation predicate in the search
    // handler at all - the global filter supplies it, so there was no line to forget.
    [Fact]
    public async Task A_search_never_reaches_another_congregation()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        // St. Brigid's canoe. Hank can find it; Priya cannot.
        var hers = await SearchAsync(priya, "canoe");

        hers.Results.ShouldBeEmpty();

        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        (await SearchAsync(hank, "canoe")).Results.ShouldNotBeEmpty();
    }

    // L2-098 AC1: Given a search term containing SQL control characters, when it is searched, then
    // it is treated as literal text and the query succeeds without error.
    [Theory]
    [InlineData("'; DROP TABLE Listings; --")]
    [InlineData("100%")]
    [InlineData("_")]
    [InlineData("[abc]")]
    public async Task A_term_of_control_characters_is_read_as_text(string term)
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync(
            $"/search?term={Uri.EscapeDataString(term)}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // A wildcard read as a wildcard would match the whole board. That is not an injection; it
        // is a wrong answer, and just as much a defect.
        (await response.ReadAsync<SearchPageBody>()).Results.ShouldBeEmpty();

        // And the board is still there.
        var board = await (await priya.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task An_empty_term_is_refused_naming_the_field()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync("/search?term=", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Named, whichever of binding and validation caught it first - what matters to a member
        // is being told which field, not which layer.
        (await response.ReadInvalidFieldsAsync())
            .ShouldContain(field => field.Equals("term", StringComparison.OrdinalIgnoreCase));
    }

    private static object ASellListing(string title, decimal price) => new
    {
        title,
        description = "In good order.",
        category = "Tools",
        neighbourhood = SeedData.Marion.Neighbourhood,
        condition = "Good",
        price,
    };

    private static async Task PostAsync(HttpClient client, string kind, object listing)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{kind}",
            listing,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<SearchPageBody> SearchAsync(
        HttpClient client,
        string term,
        string? kind = null,
        string? neighbourhood = null,
        decimal? maxPrice = null)
    {
        var query = $"/search?term={Uri.EscapeDataString(term)}";

        if (kind is not null)
        {
            query += $"&kind={kind}";
        }

        if (neighbourhood is not null)
        {
            query += $"&neighbourhood={Uri.EscapeDataString(neighbourhood)}";
        }

        if (maxPrice is not null)
        {
            query += $"&maxPrice={maxPrice}";
        }

        var response = await client.GetAsync(query, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<SearchPageBody>();
    }
}
