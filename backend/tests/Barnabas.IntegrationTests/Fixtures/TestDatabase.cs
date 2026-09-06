using Microsoft.Data.SqlClient;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// A SQL Server database of its own, for one run of the suite.
/// </summary>
/// <remarks>
/// Named uniquely rather than shared. A run that crashes half way leaves its tables behind, and
/// the next run would inherit them and fail somewhere unrelated to whatever broke.
/// <para>
/// A real SQL Server rather than an in-process database, because that is what lets the concurrency
/// criteria run at all: they turn on a real row version, a real filtered unique index and a real
/// second writer, and nothing in-process can express any of the three. See ADR-0001.
/// </para>
/// <para>
/// The instance is a developer's SQL Express by default and is overridable by environment, so the
/// same suite runs unchanged against a container in continuous integration. The variable carries
/// everything but the database name, which this type supplies.
/// </para>
/// </remarks>
public sealed class TestDatabase : IAsyncDisposable
{
    /// <summary>Where SQL Server is, with no database named. See <c>BARNABAS_TEST_SQL</c>.</summary>
    private static readonly string Instance =
        Environment.GetEnvironmentVariable("BARNABAS_TEST_SQL") is { Length: > 0 } configured
            ? configured.TrimEnd(';')
            : @"Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _name = $"BarnabasTests_{Guid.NewGuid():N}";

    public string ConnectionString => $"{Instance};Database={_name}";

    public async ValueTask DisposeAsync()
    {
        // Single-user first: a connection left open in the pool would otherwise block the drop.
        await ExecuteAsync(
            $"""
             IF DB_ID('{_name}') IS NOT NULL
             BEGIN
                 ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                 DROP DATABASE [{_name}];
             END
             """);
    }

    private static async Task ExecuteAsync(string sql)
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection($"{Instance};Database=master");

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }
}
