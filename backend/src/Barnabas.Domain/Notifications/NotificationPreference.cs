using Barnabas.Domain.Common;

namespace Barnabas.Domain.Notifications;

/// <summary>
/// One member's answer to whether they want to hear about one kind of thing.
/// </summary>
/// <remarks>
/// A row exists only where somebody has expressed a preference. Absence means enabled, which is
/// why a new kind arrives switched on for everybody without a backfill, and why a congregation
/// that has never touched its settings has no rows at all.
/// </remarks>
public sealed class NotificationPreference : ITenantOwned
{
    private NotificationPreference()
    {
    }

    public NotificationPreference(Guid congregationId, Guid memberId, NotificationKind kind, bool enabled)
    {
        CongregationId = congregationId;
        MemberId = memberId;
        Kind = kind;
        Enabled = enabled;
    }

    public Guid CongregationId { get; private set; }

    public Guid MemberId { get; private set; }

    public NotificationKind Kind { get; private set; }

    public bool Enabled { get; private set; }

    public void Set(bool enabled) => Enabled = enabled;
}
