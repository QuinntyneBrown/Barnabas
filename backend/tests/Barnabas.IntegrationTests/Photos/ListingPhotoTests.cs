using System.Net;
using System.Net.Http.Headers;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Photos;

// Acceptance Test
// Traces to: L2-032, L2-102, L2-106
// Description: A goods listing takes one photo, of a permitted type and a bounded size. What is
// stored is never what was uploaded: it is decoded, bounded and re-encoded, so nothing embedded in
// the original survives. The board is served a smaller rendition than the listing screen, and both
// leave through an endpoint that names their type rather than from a folder anything could execute.
public sealed class ListingPhotoTests : AcceptanceTest
{
    public ListingPhotoTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-032 AC1: Given a goods listing, when a JPEG of 2 MB is attached, then it is stored and
    // returned with the listing.
    [Fact]
    public async Task A_two_megabyte_jpeg_is_accepted_and_comes_back_with_the_listing()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var jpeg = Images.Jpeg(2_100_000);

        jpeg.Length.ShouldBeGreaterThan(2 * 1024 * 1024);

        var attached = await AttachAsync(marion, SeedData.Listings.Ladder, jpeg, "image/jpeg");

        attached.Url.ShouldBe($"/photos/{attached.PhotoId}");
        attached.BoardUrl.ShouldBe($"/photos/{attached.PhotoId}/board");

        // On the listing, and on the board, without either screen having to ask a second endpoint.
        var detail = await (await marion.GetAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken)).ReadAsync<ListingDetailBody>();

        detail.PhotoUrl.ShouldBe(attached.Url);

        var board = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.Single(listing => listing.ListingId == SeedData.Listings.Ladder)
            .PhotoUrl.ShouldBe(attached.BoardUrl);
    }

    // L2-032 AC2: Given a goods listing, when a file of 12 MB is attached, then the response is 413
    // Payload Too Large and nothing is stored.
    [Fact]
    public async Task Twelve_megabytes_is_refused_and_nothing_is_stored()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await AttachingAsync(
            marion,
            SeedData.Listings.Ladder,
            Images.Jpeg(12 * 1024 * 1024),
            "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);

        (await PhotoOfAsync(SeedData.Listings.Ladder)).ShouldBeNull();
    }

    // The 1 MB cap L2-096 AC2 asks for still stands everywhere else. One number could not have
    // satisfied both requirements, so the limit is a property of the endpoint.
    [Fact]
    public async Task The_ordinary_body_limit_is_unchanged_by_the_upload_route()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            "/listings/lend",
            new
            {
                title = "A ladder",
                description = new string('x', 1_200_000),
                category = "Tools",
                neighbourhood = SeedData.Marion.Neighbourhood,
                returnBy = "2026-12-01",
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    // L2-032 AC3: Given a goods listing, when a file whose content is not a permitted image type is
    // attached, then the response is 415 Unsupported Media Type.
    [Fact]
    public async Task Something_that_is_not_an_image_is_refused()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await AttachingAsync(
            marion,
            SeedData.Listings.Ladder,
            "<?php system($_GET['c']); ?>"u8.ToArray(),
            "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);

        (await PhotoOfAsync(SeedData.Listings.Ladder)).ShouldBeNull();
    }

    // L2-102 AC1: Given an uploaded file whose declared type differs from its content, when it is
    // submitted, then it is rejected with 415 Unsupported Media Type.
    [Fact]
    public async Task A_declared_type_that_does_not_match_the_bytes_is_refused()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        // Real, valid PNG bytes, declared as a JPEG. Nothing is wrong with the file; what is wrong
        // is the claim about it, and that is what the requirement refuses.
        var response = await AttachingAsync(
            marion,
            SeedData.Listings.Ladder,
            Images.Png(),
            "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    // L2-102 AC2: Given an uploaded image, when it is stored, then it is re-encoded and stripped of
    // embedded metadata.
    [Fact]
    public async Task What_is_stored_is_re_encoded_and_carries_nothing_that_was_embedded()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        // A JPEG carrying an EXIF block with a GPS-looking string in it. Somebody uploading a
        // photograph of a ladder in their garden should not be publishing where the garden is.
        var withMetadata = Images.JpegWithExif("GPSLatitude=43.6532;GPSLongitude=-79.3832");

        var attached = await AttachAsync(marion, SeedData.Listings.Ladder, withMetadata, "image/jpeg");

        var stored = await BytesOfAsync(marion, attached.Url);

        // Not the bytes that were sent. A re-encode writes a new file from decoded pixels, so
        // there is no metadata block to strip because none is carried across.
        stored.ShouldNotBe(withMetadata);
        System.Text.Encoding.ASCII.GetString(stored).ShouldNotContain("GPSLatitude");

        // Still a JPEG, and still an image.
        stored[..3].ShouldBe(new byte[] { 0xFF, 0xD8, 0xFF });
    }

    // L2-102 AC3: Given an uploaded image, when it is served, then it is served from a path that
    // cannot execute and with a Content-Type matching its content.
    [Fact]
    public async Task A_photo_is_served_with_its_own_type_and_refuses_to_be_sniffed_as_anything_else()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var attached = await AttachAsync(marion, SeedData.Listings.Ladder, Images.Jpeg(60_000), "image/jpeg");

        var response = await marion.GetAsync(attached.Url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");

        // It leaves through an endpoint that names its own type, and the browser is told not to
        // second-guess that. Nothing here is mapped as static content, so there is no path under
        // which an uploaded file could be executed.
        response.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).ShouldBeTrue();
        nosniff!.ShouldContain("nosniff");

        // Private, not public. The identifier is a capability, and a shared cache holding a
        // congregation's photographs is not something anybody agreed to.
        response.Headers.CacheControl?.Private.ShouldBeTrue();
    }

    // L2-106 AC1: Given an uploaded listing image, when it is requested at board size, then a
    // resized derivative is served rather than the original.
    [Fact]
    public async Task The_board_is_served_a_smaller_rendition_than_the_listing_screen()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var attached = await AttachAsync(marion, SeedData.Listings.Ladder, Images.Jpeg(400_000), "image/jpeg");

        var full = await BytesOfAsync(marion, attached.Url);
        var board = await BytesOfAsync(marion, attached.BoardUrl);

        board.Length.ShouldBeLessThan(full.Length);

        // A mosaic of thirty placards fetching thirty full-size photographs is the thing this
        // requirement exists to prevent, so the difference has to be worth having.
        board.Length.ShouldBeLessThan(full.Length / 2);
    }

    // Help offers time rather than a thing, so there is nothing to photograph. L2-030 says so from
    // the posting side; this is the same rule from the other one.
    [Fact]
    public async Task A_help_listing_takes_no_photo()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var posted = await marion.PostJsonAsync(
            "/listings/help",
            new
            {
                title = "Lifts to the Sunday service",
                description = "Room for three, from anywhere along the Danforth.",
                category = "Transport",
                neighbourhood = SeedData.Marion.Neighbourhood,
                windows = new[] { new { day = "Sunday", startsAt = "08:30", endsAt = "10:30" } },
            },
            TestContext.Current.CancellationToken);

        posted.StatusCode.ShouldBe(HttpStatusCode.Created);

        var listingId = (await posted.ReadAsync<PostedListing>()).ListingId;

        var response = await AttachingAsync(marion, listingId, Images.Jpeg(50_000), "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // A photo is the owner's to attach. The ownership marker is what says so, and it is the same
    // one editing and closing out declare - L2-041.
    [Fact]
    public async Task Somebody_elses_listing_cannot_be_photographed()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await AttachingAsync(
            priya,
            SeedData.Listings.Ladder,
            Images.Jpeg(50_000),
            "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await PhotoOfAsync(SeedData.Listings.Ladder)).ShouldBeNull();
    }

    // A listing in another congregation is absent rather than refused, as every cross-congregation
    // write is - L2-091.
    [Fact]
    public async Task A_listing_in_another_congregation_cannot_be_photographed()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await AttachingAsync(
            marion,
            SeedData.Listings.Canoe,
            Images.Jpeg(50_000),
            "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // At most one. A second attachment replaces the first rather than adding to it, and the bytes
    // that were displaced are deleted rather than left orphaned in the store.
    [Fact]
    public async Task A_second_photo_replaces_the_first()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var first = await AttachAsync(marion, SeedData.Listings.Ladder, Images.Jpeg(50_000), "image/jpeg");
        var second = await AttachAsync(marion, SeedData.Listings.Ladder, Images.Jpeg(60_000), "image/jpeg");

        second.PhotoId.ShouldNotBe(first.PhotoId);

        (await PhotoOfAsync(SeedData.Listings.Ladder)).ShouldBe(second.PhotoId);

        // The displaced bytes are gone, not merely unreferenced. The store keeps no reference
        // count, so an orphan nobody deletes is an orphan forever.
        (await marion.GetAsync(first.Url, TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_photo_nobody_uploaded_is_not_found()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        (await marion.GetAsync($"/photos/{Guid.NewGuid()}", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<AttachedPhoto> AttachAsync(
        HttpClient client,
        Guid listingId,
        byte[] bytes,
        string contentType)
    {
        var response = await AttachingAsync(client, listingId, bytes, contentType);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return await response.ReadAsync<AttachedPhoto>();
    }

    private static async Task<HttpResponseMessage> AttachingAsync(
        HttpClient client,
        Guid listingId,
        byte[] bytes,
        string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);

        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        content.Add(file, "file", "photo.jpg");

        return await client.PostAsync(
            $"/listings/{listingId}/photo",
            content,
            TestContext.Current.CancellationToken);
    }

    private static async Task<byte[]> BytesOfAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
    }

    private Task<Guid?> PhotoOfAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .Where(listing => listing.Id == listingId)
            .Select(listing => listing.PhotoId)
            .SingleAsync());
}
