using Barnabas.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CongregationId).IsRequired();
        builder.Property(l => l.OwnerId).IsRequired();
        builder.Property(l => l.Kind).HasConversion<int>();
        builder.Property(l => l.Status).HasConversion<int>();
        builder.Property(l => l.Title).IsRequired().HasMaxLength(Listing.TitleMaxLength);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(Listing.DescriptionMaxLength);
        builder.Property(l => l.Category).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Neighbourhood).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Price).HasPrecision(18, 2);
        builder.Property(l => l.Condition).HasMaxLength(Listing.ConditionMaxLength);

        // The terms a kind carries live with the listing rather than in a table of their own:
        // a loan's return date is not a thing that exists apart from the loan.
        builder.OwnsOne(l => l.LoanTerms, terms => terms.Property(t => t.ReturnBy).HasColumnName("ReturnBy"));

        // A Help listing's windows are a table rather than a column, because a request names
        // one by identifier and a delimited column has nothing to name.
        builder.OwnsMany(l => l.AvailabilityWindows, windows =>
        {
            windows.ToTable("AvailabilityWindows");
            windows.WithOwner().HasForeignKey("ListingId");
            windows.HasKey(w => w.Id);
            windows.Property(w => w.Id).ValueGeneratedNever();
            windows.Property(w => w.Day).HasConversion<int>();
        });

        // The board reads active listings of one congregation, newest first, and this is the
        // index that serves it.
        builder.HasIndex(l => new { l.CongregationId, l.Status, l.PostedAt });
        builder.HasIndex(l => l.OwnerId);
    }
}
