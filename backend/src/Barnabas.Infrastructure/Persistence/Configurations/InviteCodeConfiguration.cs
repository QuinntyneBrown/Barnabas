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
        // Globally unique, not per congregation. Redemption is anonymous and looks a code up
        // before any congregation is known, so two congregations holding one code would make the
        // lookup ambiguous - the same reason a member's email address is globally unique.
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
