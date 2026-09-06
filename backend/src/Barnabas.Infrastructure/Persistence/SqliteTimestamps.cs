using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// Makes timestamps sortable on SQLite.
/// </summary>
/// <remarks>
/// SQLite stores a <see cref="DateTimeOffset"/> as text carrying its offset, and refuses to
/// order by one - which would rule out the board's newest-first ordering, a thread's messages in
/// time order, and every other place the product reads in the order things happened.
/// <para>
/// Everything here is already UTC, so nothing is lost by storing the instant as a UTC
/// <see cref="DateTime"/>: the text is then lexicographically sortable and the comparison the
/// database makes is the comparison the domain means. PostgreSQL needs none of this and keeps
/// its native timestamptz.
/// </para>
/// </remarks>
public static class SqliteTimestamps
{
    private static readonly ValueConverter<DateTimeOffset, DateTime> Converter = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(Converter);
                }
            }
        }
    }
}
