using Barnabas.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class MessageThreadConfiguration : IEntityTypeConfiguration<MessageThread>
{
    public void Configure(EntityTypeBuilder<MessageThread> builder)
    {
        builder.ToTable("MessageThreads");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CongregationId).IsRequired();
        builder.Property(t => t.ListingId).IsRequired();
        builder.Property(t => t.OwnerId).IsRequired();
        builder.Property(t => t.RequesterId).IsRequired();

        // One thread per accepted request, as a constraint rather than an intention. Two
        // callers racing to accept would otherwise each create a valid thread, and a request
        // holding two threads is indistinguishable at the data layer from one holding none.
        builder.HasIndex(t => t.RequestId).IsUnique();

        // Both parties read their own thread list, so both axes are indexed.
        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.RequesterId);

        builder.HasMany(t => t.Messages)
            .WithOne()
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ReadMarks)
            .WithOne()
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        // The aggregate owns both collections through backing fields; nothing outside it may
        // add a message or move a read mark, and the mapping keeps that true.
        builder.Metadata.FindNavigation(nameof(MessageThread.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(MessageThread.ReadMarks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
