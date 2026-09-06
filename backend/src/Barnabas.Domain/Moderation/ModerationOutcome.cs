namespace Barnabas.Domain.Moderation;

/// <summary>What a moderator decided about a reported listing.</summary>
public enum ModerationOutcome
{
    /// <summary>Nothing wrong with it. The flag is cleared and it stays on the board.</summary>
    Approved = 0,

    /// <summary>It comes off the board, and its owner is told.</summary>
    Removed = 1,
}
