using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Access;

// Acceptance Test
// Traces to: L2-019
// Description: Signing out ends the session, and both token kinds stop being accepted at once,
// while a session on another device keeps working.
public sealed class SignOutTests : AcceptanceTest
{
    public SignOutTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-019 AC1: Given a signed-in member, when they sign out and then present the same refresh
    // token, then the response is 401.
    [Fact]
    public async Task The_refresh_token_stops_working()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        var token = device.RefreshToken;

        (await device.SignOutAsync()).ShouldBe(HttpStatusCode.NoContent);

        (await device.RenewWithAsync(token)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-019 AC4: Given a signed-in member, when they sign out and then present the access token
    // issued for that session, then the response is 401.
    [Fact]
    public async Task The_access_token_stops_working_immediately()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);

        (await device.SignOutAsync()).ShouldBe(HttpStatusCode.NoContent);

        // Unexpired and correctly signed, and refused anyway. A signature cannot express
        // revocation, so the session is read on every request rather than waited out.
        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-019 AC5: Given a session whose refresh token has been rotated at least once, when the
    // member signs out and presents the most recent access token, then the response is 401.
    [Fact]
    public async Task A_rotated_session_signs_out_just_as_completely()
    {
        var device = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        (await device.RenewAsync()).ShouldBe(HttpStatusCode.OK);
        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);

        (await device.SignOutAsync()).ShouldBe(HttpStatusCode.NoContent);

        (await device.ReadBoardAsync()).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // L2-019 AC6: Given a member signed in on two devices, when they sign out on one, then the
    // session on the other remains usable.
    [Fact]
    public async Task Signing_out_on_one_device_leaves_the_other_signed_in()
    {
        var phone = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);
        var laptop = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        (await phone.SignOutAsync()).ShouldBe(HttpStatusCode.NoContent);

        (await phone.ReadBoardAsync()).ShouldBe(HttpStatusCode.Unauthorized);
        (await laptop.ReadBoardAsync()).ShouldBe(HttpStatusCode.OK);
    }
}
