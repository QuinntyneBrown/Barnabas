using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Access;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Budgets;

/// <summary>
/// The congregations, members and listings the measurements run against.
/// </summary>
/// <remarks>
/// Written straight to the database rather than through the API. Five hundred listings posted one
/// at a time over HTTP would take longer than the measurements themselves, and what is being
/// measured is how the board reads at scale rather than how quickly it can be filled.
/// <para>
/// Its own congregations, not the seeded ones. A harness that filled St. Aidan's would leave five
/// hundred listings behind for the acceptance suites to trip over, and the whole point of
/// <c>L2-107</c> is that one congregation's load does not reach another's.
/// </para>
/// </remarks>
public sealed class Population
{
    /// <summary>Everything this harness creates is named so it can be found and removed again.</summary>
    private const string Marker = "budget-harness";

    private readonly string _connectionString;

    public Population(string connectionString) => _connectionString = connectionString;

    /// <summary>One congregation, its member, and the token that member holds.</summary>
    public sealed record Parish(Guid CongregationId, Guid MemberId, string AccessToken);

    public async Task<IReadOnlyList<Parish>> ArrangeAsync(
        int congregations,
        int listingsEach,
        Func<Member, Session, string> issueToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueToken);

        await RemoveAsync(cancellationToken);

        var parishes = new List<Parish>(congregations);
        var now = DateTimeOffset.UtcNow;

        await using var context = Open();

        for (var index = 0; index < congregations; index++)
        {
            var congregation = Congregation.Provision(
                Guid.NewGuid(),
                $"{Marker} {index}",
                $"{Marker}-{index}-{Guid.NewGuid():n}",
                ["Riverdale", "Leslieville"]);

            var member = new Member(
                Guid.NewGuid(),
                congregation.Id,
                $"{Marker}-{index}-{Guid.NewGuid():n}@example.invalid",
                $"Member {index}",
                "Riverdale",
                MemberRole.Member,
                MemberStatus.Approved);

            var session = new Session(Guid.NewGuid(), congregation.Id, member.Id, now);

            context.Add(congregation);
            context.Add(member);
            context.Add(session);

            for (var listing = 0; listing < listingsEach; listing++)
            {
                context.Add(Listing.PostLend(
                    Guid.NewGuid(),
                    congregation.Id,
                    member.Id,

                    // Varied enough that a search has something to find and something to skip.
                    // A board of five hundred identical rows would measure a query plan that no
                    // real congregation produces.
                    $"{Words[listing % Words.Length]} number {listing} for {Marker}",
                    $"A {Words[(listing + 3) % Words.Length]} in good order, posted for measurement.",
                    "Tools",
                    "Riverdale",
                    DateOnly.FromDateTime(now.AddDays(30).UtcDateTime),
                    now.AddMinutes(-listing)));
            }

            parishes.Add(new Parish(congregation.Id, member.Id, issueToken(member, session)));
        }

        await context.SaveChangesAsync(cancellationToken);

        return parishes;
    }

    /// <summary>
    /// Removes everything the harness created, whether or not the run finished.
    /// </summary>
    /// <remarks>
    /// By marker rather than by identifier, so a run that was interrupted halfway is cleaned up by
    /// the next one instead of leaving five hundred listings for the acceptance suite to find.
    /// </remarks>
    public async Task RemoveAsync(CancellationToken cancellationToken)
    {
        await using var context = Open();

        var mine = await context.Set<Congregation>()
            .IgnoreQueryFilters()
            .Where(congregation => congregation.Name.StartsWith(Marker))
            .Select(congregation => congregation.Id)
            .ToListAsync(cancellationToken);

        if (mine.Count == 0)
        {
            return;
        }

        // Children first, in the order the foreign keys allow.
        await context.Set<Listing>().IgnoreQueryFilters()
            .Where(listing => mine.Contains(listing.CongregationId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<Session>().IgnoreQueryFilters()
            .Where(session => mine.Contains(session.CongregationId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<Member>().IgnoreQueryFilters()
            .Where(member => mine.Contains(member.CongregationId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<Congregation>().IgnoreQueryFilters()
            .Where(congregation => mine.Contains(congregation.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private BarnabasDbContext Open() =>
        new(
            new DbContextOptionsBuilder<BarnabasDbContext>().UseSqlServer(_connectionString).Options,

            // Unresolved, so every read here has to say IgnoreQueryFilters for itself. The harness
            // writes across congregations by definition, and the filter exists to stop exactly
            // that happening by accident.
            new NoCongregation());

    private static readonly string[] Words =
    [
        "ladder", "drill", "canoe", "wheelbarrow", "tent", "crib", "pushchair", "mitre saw",
        "extension lead", "trestle table", "cake tin", "projector",
    ];

    /// <summary>No congregation in scope, which is what a harness writing across all of them has.</summary>
    private sealed class NoCongregation : ICongregationContext
    {
        public bool IsResolved => false;

        public Guid CongregationId => Guid.Empty;

        public Guid MemberId => Guid.Empty;

        public Guid SessionId => Guid.Empty;

        public MemberRole Role => MemberRole.Member;

        public MemberStatus Status => MemberStatus.Approved;
    }
}
