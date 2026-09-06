using Barnabas.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ThreadId).IsRequired();
        builder.Property(m => m.SenderId).IsRequired();
        builder.Property(m => m.Body).IsRequired().HasMaxLength(Message.BodyMaxLength);

        // A message is not itself ITenantOwned - it belongs to a thread, which is. A shadow
        // congregation brings it under the same global filter anyway, so that the DbSet the
        // application layer exposes cannot be turned into a cross-tenant read by a handler
        // that queries messages directly instead of through the aggregate.
        builder.Property<Guid>(BarnabasDbContext.CongregationIdProperty).IsRequired();

        builder.HasIndex(m => new { m.ThreadId, m.SentAt });
    }
}
