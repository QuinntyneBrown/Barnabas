using Barnabas.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class ListingRequestConfiguration : IEntityTypeConfiguration<ListingRequest>
{
    public void Configure(EntityTypeBuilder<ListingRequest> builder)
    {
        builder.ToTable("ListingRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CongregationId).IsRequired();
        builder.Property(r => r.ListingId).IsRequired();
        builder.Property(r => r.RequesterId).IsRequired();
        builder.Property(r => r.Message).IsRequired().HasMaxLength(ListingRequest.MessageMaxLength);
        builder.Property(r => r.Status).HasConversion<int>();

        // A real rowversion column, which the server advances on every update. Nothing in
        // application code has to remember to move it, and that is the point: it decides an
        // accept racing a decline, and a check made before the save could not.
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.OwnsOne(r => r.LoanTerms, terms =>
        {
            terms.Property(t => t.PickupOn).HasColumnName("PickupOn");
            terms.Property(t => t.ReturnBy).HasColumnName("RequestedReturnBy");
        });

        // The one rule a policy cannot make true on its own. Two simultaneous requests both
        // pass an in-handler check before either commits, so L2-062 is settled here, in the
        // only place that can settle it. The filter is what makes it a rule about *open*
        // requests: a declined request must not block a second attempt.
        builder.HasIndex(r => new { r.ListingId, r.RequesterId })
            .IsUnique()
            .HasFilter("[Status] = 0")
            .HasDatabaseName("IX_ListingRequests_OneOpenRequestPerMember");

        builder.HasIndex(r => new { r.ListingId, r.Status });
        builder.HasIndex(r => r.RequesterId);
    }
}
