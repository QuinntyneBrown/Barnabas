using Microsoft.Data.SqlClient;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// A LocalDB database of its own, for one run of the suite.
/// </summary>
/// <remarks>
/// Named uniquely rather than shared. A run that crashes half way leaves its tables behind, and
/// the next run would inherit them and fail somewhere unrelated to whatever broke.
/// <para>
/// LocalDB rather than a container: it starts on demand, needs no runtime installed, and is what
/// lets the concurrency criteria run at all. They turn on a real row version and a real second
/// writer, and no in-process database can express either.
/// </para>
/// </remarks>
public sealed class TestDatabase : IAsyncDisposable
{
    private const string Instance = @"Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True";

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
