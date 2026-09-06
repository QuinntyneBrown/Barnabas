using Barnabas.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.CongregationId).IsRequired();
        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.Kind).HasConversion<int>();
        builder.Property(n => n.SubjectMemberDisplayName).HasMaxLength(200);
        builder.Property(n => n.ListingTitle).HasMaxLength(120);

        // Newest first for one member, which is the only way this is ever read.
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt });

        // The unread count sits on every screen at every width, so it is the most-called query in
        // the product. A filtered index means it counts a handful of rows rather than scanning a
        // member's whole history.
        builder.HasIndex(n => n.RecipientId)
            .HasFilter("[ReadAt] IS NULL")
            .HasDatabaseName("IX_Notifications_Unread");
    }
}
