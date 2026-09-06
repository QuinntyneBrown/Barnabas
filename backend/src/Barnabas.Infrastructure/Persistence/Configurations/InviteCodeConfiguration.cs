using Barnabas.Domain.Congregations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class InviteCodeConfiguration : IEntityTypeConfiguration<InviteCode>
{
    public void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.ToTable("InviteCodes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CongregationId).IsRequired();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(32);
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
