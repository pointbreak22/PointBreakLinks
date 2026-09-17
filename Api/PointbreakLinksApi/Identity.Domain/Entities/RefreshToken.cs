using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// FOXLinks keeps exactly one row per user (AuthController::issueTokens does updateOrCreate
// on user_id), rather than one row per device/session. Kept the same here for parity —
// logging in on a new device invalidates the previous refresh cookie.
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
