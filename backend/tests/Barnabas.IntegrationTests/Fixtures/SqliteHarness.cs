using Barnabas.Infrastructure.Persistence;

namespace Barnabas.IntegrationTests.Fixtures;

/// <inheritdoc />
/// <remarks>
/// A file rather than <c>:memory:</c>. An in-memory SQLite database lives only as long as the
/// connection that opened it, and the API opens a fresh connection per scope, so the schema
/// would vanish between the migration and the first request.
/// </remarks>
public sealed class SqliteHarness : DatabaseHarness
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"barnabas-tests-{Guid.NewGuid():N}.db");

    public override string Provider => DatabaseProviders.Sqlite;

    public override string ConnectionString => $"Data Source={_path}";

    public override ValueTask StartAsync() => ValueTask.CompletedTask;

    public override ValueTask DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return ValueTask.CompletedTask;
    }
}
