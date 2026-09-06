using Barnabas.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        // Keyed on the pair, so a member holds one answer per kind and saying so twice is not
        // possible rather than merely discouraged.
        builder.HasKey(p => new { p.MemberId, p.Kind });

        builder.Property(p => p.CongregationId).IsRequired();
        builder.Property(p => p.Kind).HasConversion<int>();
    }
}
