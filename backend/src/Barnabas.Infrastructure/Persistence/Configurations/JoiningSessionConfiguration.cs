using Barnabas.Domain.Congregations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class JoiningSessionConfiguration : IEntityTypeConfiguration<JoiningSession>
{
    public void Configure(EntityTypeBuilder<JoiningSession> builder)
    {
        builder.ToTable("JoiningSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CongregationId).IsRequired();
        builder.Property(s => s.InviteCodeId).IsRequired();
        builder.Property(s => s.TokenHash).IsRequired().HasMaxLength(128);

        // The lookup is by hash and nothing else, because the caller holds a token and not an
        // identifier. Unique so one hash is one entitlement.
        builder.HasIndex(s => s.TokenHash).IsUnique();
    }
}
