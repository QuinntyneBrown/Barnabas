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
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BarnabasDbContext>
{
    public BarnabasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BarnabasDbContext>()
            .UseSqlServer(@"Server=.\SQLEXPRESS;Database=Barnabas;Trusted_Connection=True;TrustServerCertificate=True")
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

        public MemberStatus Status => throw new NotSupportedException();
    }
}
