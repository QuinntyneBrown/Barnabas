using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Brings the schema up to date and seeds the congregation, once, at startup.
/// </summary>
/// <remarks>
/// Always through migrations, never <c>EnsureCreated</c>. One provider means one migration set,
/// and applying it on every start - including on every acceptance run - is what keeps it from
/// drifting away from the model unnoticed.
/// </remarks>
public sealed class DatabaseInitialiser
{
    private readonly BarnabasDbContext _context;
    private readonly CongregationSeeder _seeder;
    private readonly DatabaseOptions _options;

    public DatabaseInitialiser(
        BarnabasDbContext context,
        CongregationSeeder seeder,
        IOptions<DatabaseOptions> options)
    {
        _context = context;
        _seeder = seeder;
        _options = options.Value;
    }

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ResetOnStart)
        {
            await _context.Database.EnsureDeletedAsync(cancellationToken);
        }

        await _context.Database.MigrateAsync(cancellationToken);

        if (_options.Seed)
        {
            await _seeder.SeedAsync(cancellationToken);
        }
    }
}
