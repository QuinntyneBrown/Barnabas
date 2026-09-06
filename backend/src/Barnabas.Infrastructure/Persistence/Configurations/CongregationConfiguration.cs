using Barnabas.Domain.Congregations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class CongregationConfiguration : IEntityTypeConfiguration<Congregation>
{
    /// <summary>ASCII unit separator, which cannot occur in a neighbourhood name.</summary>
    private const char Separator = (char)31;

    public void Configure(EntityTypeBuilder<Congregation> builder)
    {
        builder.ToTable("Congregations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        // Stored as one delimited column rather than a child table. The order is part of the
        // value - members pick from the list as the parish office wrote it - and a child table
        // would need an ordinal column to preserve what a single string preserves for free.
        builder.Property(c => c.Neighbourhoods)
            .IsRequired()
            .HasConversion(
                names => string.Join(Separator, names),
                value => value.Split(Separator, StringSplitOptions.RemoveEmptyEntries),
                new ValueComparer<IReadOnlyList<string>>(
                    (left, right) => left != null && right != null && left.SequenceEqual(right),
                    names => names.Aggregate(0, (hash, name) => HashCode.Combine(hash, name.GetHashCode(StringComparison.Ordinal))),
                    names => names.ToArray()));
    }
}
