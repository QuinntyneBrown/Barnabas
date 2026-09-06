namespace Barnabas.Domain.Notifications;

/// <summary>
/// What a member has chosen to hear about.
/// </summary>
/// <remarks>
/// The rule that absence means enabled lives here rather than in a handler, so every caller reads
/// it the same way and a missing row is never mistaken for a refusal.
/// <para>
/// A moderator removing a listing is always told to its owner. <c>L2-084</c> requires it, and a
/// decision made about somebody that they could opt out of hearing would be a decision made
/// behind their back.
/// </para>
/// </remarks>
public sealed class NotificationPreferences
{
    /// <summary>The kinds a member may switch off.</summary>
    public static readonly IReadOnlySet<NotificationKind> Configurable = new HashSet<NotificationKind>
    {
        NotificationKind.RequestReceived,
        NotificationKind.RequestAccepted,
        NotificationKind.RequestDeclined,
        NotificationKind.MessageReceived,
    };

    private readonly IReadOnlyDictionary<NotificationKind, bool> _chosen;

    public NotificationPreferences(IReadOnlyDictionary<NotificationKind, bool> chosen) => _chosen = chosen;

    public bool Allows(NotificationKind kind) =>
        !Configurable.Contains(kind) || !_chosen.TryGetValue(kind, out var enabled) || enabled;
}
