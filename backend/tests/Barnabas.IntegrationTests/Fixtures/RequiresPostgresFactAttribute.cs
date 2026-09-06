using System.Runtime.CompilerServices;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Marks a test that can only be honest on PostgreSQL.
/// </summary>
/// <remarks>
/// The concurrency criteria turn on a row version and on constraint enforcement under genuine
/// parallel writers. SQLite has neither, so on SQLite these report as skipped. Passing them
/// there would be worse than skipping: it would claim a rule holds on the strength of a test
/// that could not have caught it failing.
/// </remarks>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("BARNABAS_TEST_DB"),
                "sqlite",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Requires PostgreSQL: SQLite cannot express a row version or a concurrent writer.";
        }
    }
}
