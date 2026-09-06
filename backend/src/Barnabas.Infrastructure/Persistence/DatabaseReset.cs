using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Puts the database back to the seeded congregation.
/// </summary>
/// <remarks>
/// Both acceptance suites need this and for the same reason: a test that depends on what the
/// test before it left behind is a test that passes in one order and fails in another. The
/// integration suite calls it between tests; the Playwright suite reaches it through an endpoint
/// that exists only in Development.
/// <para>
/// The order is children before parents. Cascade deletes would cover most of it, but stating the
/// order means this does not quietly depend on which relationships happen to be configured to
/// cascade today.
/// </para>
/// </remarks>
public sealed class DatabaseReset
{
    private static readonly string[] TablesInDeletionOrder =
    [
        "Messages",
        "ThreadReadMarks",
        "MessageThreads",
        "ListingRequests",
        "Listings",
        "RefreshTokens",
        "Sessions",
        "SignInTokens",
        "InviteCodes",
        "Members",
        "Congregations",
    ];

    private readonly BarnabasDbContext _context;
    private readonly CongregationSeeder _seeder;

    public DatabaseReset(BarnabasDbContext context, CongregationSeeder seeder)
    {
        _context = context;
        _seeder = seeder;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        foreach (var table in TablesInDeletionOrder)
        {
            // The names are the fixed list above, not anything a caller supplies.
#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"", cancellationToken);
#pragma warning restore EF1002
        }

        await _seeder.SeedAsync(cancellationToken);
    }
}
