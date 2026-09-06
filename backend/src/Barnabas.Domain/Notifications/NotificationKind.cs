namespace Barnabas.Domain.Notifications;

/// <summary>
/// What a notification is about.
/// </summary>
/// <remarks>
/// The kind is what a member turns off, so these are the granularity <c>L2-075</c> works at. They
/// are named for the event rather than for the screen, because a member deciding what to hear
/// about is thinking about what happened, not where it will take them.
/// </remarks>
public enum NotificationKind
{
    /// <summary>Somebody has asked for something of yours.</summary>
    RequestReceived = 0,

    /// <summary>Your request was accepted.</summary>
    RequestAccepted = 1,

    /// <summary>Your request was declined.</summary>
    RequestDeclined = 2,

    /// <summary>Somebody wrote to you in a thread.</summary>
    MessageReceived = 3,

    /// <summary>
    /// A moderator took your listing off the board.
    /// </summary>
    /// <remarks>
    /// Not among the kinds a member may switch off. <c>L2-084</c> requires the owner to be told,
    /// and a moderator's decision that the owner could opt out of hearing would be a decision
    /// made about them behind their back.
    /// </remarks>
    ListingRemoved = 4,
}
