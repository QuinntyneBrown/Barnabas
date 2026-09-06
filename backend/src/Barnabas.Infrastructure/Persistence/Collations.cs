namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// The collations Barnabas depends on, named in one place.
/// </summary>
/// <remarks>
/// Searching is case-insensitive because the column says so, not because the server happens to
/// have been installed that way. Declaring it means a development machine, an acceptance run and
/// a deployment all give the same answer to the same search — which is the whole argument of
/// ADR-0001, applied to a comparison rather than to a constraint.
/// </remarks>
public static class Collations
{
    /// <summary>Case- and accent-insensitive, which is how a person searching expects to be read.</summary>
    public const string CaseInsensitive = "SQL_Latin1_General_CP1_CI_AS";
}
