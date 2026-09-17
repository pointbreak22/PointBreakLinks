using Domain.Common;

namespace Domain.Entities;

// One row per user, lazily created on first access (INotificationPreferenceRepository.
// GetOrCreateAsync) — same pattern as Wallet, see its comment. Gates only the transactional
// emails this app actually sends (order lifecycle, dispute resolution — see the five call sites
// in Application/CQRS/Sites/Commands/*), both default true so existing behavior doesn't change
// for anyone who never visits the settings. Security-critical Identity emails (password reset,
// account-lockout warning) are never gated by this — those go out regardless, same as any real
// auth provider. In-app notifications (Notification/NotificationPusher) are also never gated —
// the header bell should always reflect what actually happened, only the email copy is optional.
public class NotificationPreference : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool EmailOnOrderUpdates { get; set; } = true;
    public bool EmailOnDisputeUpdates { get; set; } = true;
}
