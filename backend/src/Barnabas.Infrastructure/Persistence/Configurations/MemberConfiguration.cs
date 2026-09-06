using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CongregationId).IsRequired();
        builder.Property(m => m.EmailAddress).IsRequired().HasMaxLength(320);
        builder.Property(m => m.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Neighbourhood).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.Status).HasConversion<int>();

        // Globally unique, not unique per congregation. Signing in resolves a member from an
        // address alone, before any congregation is known, so two members sharing an address
        // would make that lookup ambiguous at exactly the moment it cannot ask for help.
        builder.HasIndex(m => m.EmailAddress).IsUnique();
        builder.HasIndex(m => m.CongregationId);
    }
}
