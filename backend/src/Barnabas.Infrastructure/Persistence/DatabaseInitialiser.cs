using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Brings the schema up to date and seeds the congregation, once, at startup.
/// </summary>
/// <remarks>
/// Migrations are the PostgreSQL story. SQLite gets the model created directly, because
/// maintaining a second migration set for a provider that exists to keep the acceptance suite
/// runnable would cost more than it returns and would drift the moment nobody looked.
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

        if (_context.Database.IsNpgsql())
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (_options.Seed)
        {
            await _seeder.SeedAsync(cancellationToken);
        }
    }
}
