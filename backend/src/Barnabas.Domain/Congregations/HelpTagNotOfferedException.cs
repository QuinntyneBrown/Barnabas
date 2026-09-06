namespace Barnabas.Domain.Congregations;

/// <summary>
/// Raised when a member declares a kind of help their congregation does not list.
/// </summary>
/// <remarks>
/// The set is configured for the same reason the neighbourhoods are: the directory is searchable
/// by tag, and free text would make two members offering the same thing unfindable by each other.
/// </remarks>
public sealed class HelpTagNotOfferedException : Exception
{
    public HelpTagNotOfferedException(string tag)
        : base("That is not one of your congregation's kinds of help.") => Tag = tag;

    public string Tag { get; }
}
