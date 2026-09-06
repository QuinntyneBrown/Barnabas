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
        builder.Property(c => c.Name).IsRequired().HasMaxLength(Congregation.NameMaxLength);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(Congregation.SlugMaxLength);

        // The rule that makes L2-001 AC2 true. Two administrators posting the same slug both pass
        // an in-handler check before either commits; only the index refuses the second, and the
        // handler turns its refusal into the same 409 the ordinary case gets.
        //
        // A plain unique index rather than a case-insensitive one, because the slug is lower-cased
        // by the factory - so uniqueness does not depend on the server's collation.
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("IX_Congregations_Slug");

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
