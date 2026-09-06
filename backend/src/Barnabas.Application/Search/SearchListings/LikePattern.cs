namespace Barnabas.Application.Search.SearchListings;

/// <summary>
/// Turns what a member typed into a pattern that means what they typed.
/// </summary>
/// <remarks>
/// A search term is member text, and member text is data. Parameterisation already stops it being
/// read as SQL; this stops it being read as a <em>pattern</em> — without it, a member searching
/// for "100%" would match everything, and one searching for "_" would match every single
/// character. That is not an injection, it is a wrong answer, and it is just as much a defect.
/// </remarks>
public static class LikePattern
{
    /// <summary>
    /// The character that makes the next one literal.
    /// </summary>
    /// <remarks>
    /// A backslash, declared to SQL Server explicitly rather than relied on: <c>LIKE</c> has no
    /// default escape character, so one that is not named does not exist.
    /// </remarks>
    public const string Escape = @"\";

    /// <summary>Wraps a term so it matches anywhere in the text, taking it literally.</summary>
    public static string Containing(string term) => $"%{Literal(term)}%";

    private static string Literal(string term) => term
        .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
        .Replace("%", Escape + "%", StringComparison.Ordinal)
        .Replace("_", Escape + "_", StringComparison.Ordinal)
        .Replace("[", Escape + "[", StringComparison.Ordinal);
}
