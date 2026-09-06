using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Gets two members as far as an open thread, which is where the handoff is arranged.
/// </summary>
/// <remarks>
/// A thread exists only as a consequence of an accepted request - there is no endpoint that
/// creates one - so every messaging test has to walk the same path to reach one.
/// </remarks>
public static class Handoff
{
    public static async Task<Guid> OpenThreadAsync(
        BarnabasApiFactory api,
        Guid listingId,
        Guid ownerId,
        Guid requesterId)
    {
        ArgumentNullException.ThrowIfNull(api);

        using var requester = await api.ClientForAsync(requesterId);

        var made = await (await requester.PostJsonAsync(
            $"/listings/{listingId}/requests/loan",
            Requests.ToBorrow())).ReadAsync<MadeRequest>();

        using var owner = await api.ClientForAsync(ownerId);

        var accepted = await (await owner.PostJsonAsync(
            $"/requests/{made.RequestId}/accept",
            new { })).ReadAsync<AcceptedRequest>();

        return accepted.ThreadId;
    }

    /// <summary>The ladder thread, between Marion who owns it and Priya who asked for it.</summary>
    public static Task<Guid> AboutTheLadderAsync(BarnabasApiFactory api) =>
        OpenThreadAsync(api, SeedData.Listings.Ladder, SeedData.Marion.Id, SeedData.Priya.Id);
}
