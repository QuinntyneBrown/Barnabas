using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Builds a context for the migrations tool, which has no request and therefore no caller.
/// </summary>
/// <remarks>
/// Migrations describe the schema, and the schema is the same for every congregation, so the
/// unresolved context this supplies is not a gap - there is nothing here for a filter to do.
/// PostgreSQL is always the target: SQLite is created from the model rather than migrated.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BarnabasDbContext>
{
    public BarnabasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BarnabasDbContext>()
            .UseNpgsql("Host=localhost;Database=barnabas;Username=barnabas;Password=barnabas")
            .Options;

        return new BarnabasDbContext(options, new UnresolvedCongregationContext());
    }

    private sealed class UnresolvedCongregationContext : ICongregationContext
    {
        public bool IsResolved => false;

        public Guid CongregationId => Guid.Empty;

        public Guid MemberId => Guid.Empty;

        public Guid SessionId => Guid.Empty;

        public MemberRole Role => MemberRole.Member;
    }
}
