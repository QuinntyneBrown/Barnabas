namespace Barnabas.Infrastructure.Persistence;

/// <summary>Which database the API runs against, and how it is brought up.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Where SQL Server is.
    /// </summary>
    /// <remarks>
    /// Defaulted to LocalDB, which starts on demand and needs no service running or container
    /// pulled. It is a seam rather than an assumption: point this at SQL Express, or at a
    /// deployed instance, and nothing else changes.
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Drops and recreates the schema at startup. For the Playwright suite only.</summary>
    public bool ResetOnStart { get; set; }

    public bool Seed { get; set; }
}
