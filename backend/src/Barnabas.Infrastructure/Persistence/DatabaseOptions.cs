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

    /// <summary>
    /// Applies the migrations when the API starts.
    /// </summary>
    /// <remarks>
    /// On by default, because a schema that drifts from the model unnoticed is the failure this
    /// prevents. Off is for a deployment that applies migrations from a job of its own - and for
    /// the one acceptance test that starts the API against a database deliberately not there,
    /// which could not otherwise reach the health endpoint to find out what it says.
    /// </remarks>
    public bool MigrateOnStart { get; set; } = true;
}
