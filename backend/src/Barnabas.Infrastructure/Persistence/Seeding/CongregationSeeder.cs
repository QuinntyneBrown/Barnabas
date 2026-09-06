using Barnabas.Domain.Congregations;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence.Seeding;

/// <summary>
/// Puts the seeded congregation into an empty database, and leaves a populated one alone.
/// </summary>
/// <remarks>
/// Every read here ignores the global query filter. Seeding runs outside a request, so no
/// congregation is in scope, and a filtered read would find nothing and seed a second time on
/// every start.
/// </remarks>
public sealed class CongregationSeeder
{
    private readonly BarnabasDbContext _context;
    private readonly TimeProvider _time;

    public CongregationSeeder(BarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Set<Congregation>().IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = _time.GetUtcNow();

        _context.AddRange(
            Congregation.Provision(
                SeedData.Platform.Id,
                SeedData.Platform.Name,
                SeedData.Platform.Slug,
                SeedData.Platform.Neighbourhoods),
            Congregation.Provision(
                SeedData.StAidans.Id,
                SeedData.StAidans.Name,
                SeedData.StAidans.Slug,
                SeedData.StAidans.Neighbourhoods),
            Congregation.Provision(
                SeedData.StBrigids.Id,
                SeedData.StBrigids.Name,
                SeedData.StBrigids.Slug,
                SeedData.StBrigids.Neighbourhoods));

        _context.AddRange(
            new InviteCode(
                SeedData.StAidans.InviteCodeId,
                SeedData.StAidans.Id,
                SeedData.StAidans.InviteCode,
                now.AddYears(1)),
            new InviteCode(
                SeedData.StBrigids.InviteCodeId,
                SeedData.StBrigids.Id,
                SeedData.StBrigids.InviteCode,
                now.AddYears(1)));

        _context.AddRange(
            Approved(SeedData.Priya.Id, SeedData.StAidans.Id, SeedData.Priya.EmailAddress, SeedData.Priya.DisplayName, SeedData.Priya.Neighbourhood),
            Approved(SeedData.Grace.Id, SeedData.StAidans.Id, SeedData.Grace.EmailAddress, SeedData.Grace.DisplayName, SeedData.Grace.Neighbourhood),
            Approved(SeedData.Hank.Id, SeedData.StBrigids.Id, SeedData.Hank.EmailAddress, SeedData.Hank.DisplayName, SeedData.Hank.Neighbourhood),

            // The administrator, in the platform congregation. Everything a congregation needs to
            // exist comes from somebody holding this role, so a deployment with none could never
            // provision its first parish. ADR-0002.
            WithRole(
                SeedData.Ada.Id,
                SeedData.Platform.Id,
                SeedData.Ada.EmailAddress,
                SeedData.Ada.DisplayName,
                SeedData.Ada.Neighbourhood,
                MemberRole.Administrator),

            // A moderator of St. Aidan's, so issuing an invite and reviewing the queue have
            // somebody entitled to do them.
            WithRole(
                SeedData.Marion.Id,
                SeedData.StAidans.Id,
                SeedData.Marion.EmailAddress,
                SeedData.Marion.DisplayName,
                SeedData.Marion.Neighbourhood,
                MemberRole.Moderator));

        _context.AddRange(
            Listing.PostLend(
                SeedData.Listings.Ladder,
                SeedData.StAidans.Id,
                SeedData.Marion.Id,
                SeedData.Listings.LadderTitle,
                "Well used, sound. Both spreaders lock properly.",
                "Tools",
                SeedData.Marion.Neighbourhood,
                DateOnly.FromDateTime(now.AddDays(14).UtcDateTime),
                now.AddDays(-4)),
            Listing.PostSell(
                SeedData.Listings.Drill,
                SeedData.StAidans.Id,
                SeedData.Marion.Id,
                SeedData.Listings.DrillTitle,
                "Two batteries and a charger. Barely used since the deck went in.",
                "Tools",
                SeedData.Marion.Neighbourhood,
                "Good",
                45.00m,
                now.AddDays(-2)),
            Listing.PostLend(
                SeedData.Listings.Canoe,
                SeedData.StBrigids.Id,
                SeedData.Hank.Id,
                SeedData.Listings.CanoeTitle,
                "Out until Labour Day, then free most weekends.",
                "Outdoor",
                SeedData.Hank.Neighbourhood,
                DateOnly.FromDateTime(now.AddDays(21).UtcDateTime),
                now.AddDays(-9)));

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Member Approved(
        Guid id,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood) =>
        new(id, congregationId, emailAddress, displayName, neighbourhood, SeedData.DefaultRole, MemberStatus.Approved);

    private static Member WithRole(
        Guid id,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood,
        MemberRole role) =>
        new(id, congregationId, emailAddress, displayName, neighbourhood, role, MemberStatus.Approved);
}
