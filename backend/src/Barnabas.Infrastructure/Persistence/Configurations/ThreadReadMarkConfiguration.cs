using Barnabas.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class ThreadReadMarkConfiguration : IEntityTypeConfiguration<ThreadReadMark>
{
    public void Configure(EntityTypeBuilder<ThreadReadMark> builder)
    {
        builder.ToTable("ThreadReadMarks");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ThreadId).IsRequired();
        builder.Property(m => m.MemberId).IsRequired();

        // Shadow congregation, for the same reason as Message.
        builder.Property<Guid>(BarnabasDbContext.CongregationIdProperty).IsRequired();

        // One mark per member per thread. Unread is a property of the reader, so a second row
        // for the same reader would make "has this member read it" ambiguous.
        builder.HasIndex(m => new { m.ThreadId, m.MemberId }).IsUnique();
    }
}
