namespace Barnabas.Infrastructure.Persistence;

/// <summary>Which database the API runs against, and how it is brought up.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// <c>postgres</c> or <c>sqlite</c>.
    /// </summary>
    /// <remarks>
    /// PostgreSQL is the production provider and the one the concurrency rules are expressed
    /// against. SQLite exists so the acceptance suites can run on a machine with no container
    /// runtime; the tests that depend on a real row version declare that they need PostgreSQL
    /// rather than passing vacuously.
    /// </remarks>
    public string Provider { get; set; } = DatabaseProviders.Postgres;

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Drops and recreates the schema at startup. For the Playwright suite only.</summary>
    public bool ResetOnStart { get; set; }

    public bool Seed { get; set; }
}
