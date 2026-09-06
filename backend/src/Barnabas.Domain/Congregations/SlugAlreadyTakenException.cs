namespace Barnabas.Domain.Congregations;

/// <summary>
/// Raised when a congregation is provisioned with a slug another already holds.
/// </summary>
/// <remarks>
/// A slug is how a congregation is named in an address, so two congregations holding one would
/// make the address ambiguous. The unique index is what makes the rule true under concurrency;
/// this is what the handler raises when either the check or the index refuses.
/// </remarks>
public sealed class SlugAlreadyTakenException : Exception
{
    public SlugAlreadyTakenException(string slug)
        : base("Another congregation already uses that slug.") => Slug = slug;

    public string Slug { get; }
}
