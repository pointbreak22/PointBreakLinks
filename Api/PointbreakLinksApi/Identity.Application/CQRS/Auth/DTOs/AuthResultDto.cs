namespace Identity.Application.CQRS.Auth.DTOs;

// RefreshToken/RefreshTokenExpiresAt are handed back (rather than set as a cookie here)
// because Application has no notion of HTTP — the controller is the one that turns this
// into an httpOnly Set-Cookie, same split FOXLinks' AuthController::issueTokens() did.
// TrustedDeviceToken/TrustedDeviceTokenExpiresAt are only ever set by
// CompleteTwoFactorLoginCommandHandler when the user checked "Запомнить это устройство" — every
// other caller of TokenIssuer.IssueAsync leaves them null, and the controller only writes the
// trusted-device cookie when they're present.
public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User,
    string? TrustedDeviceToken = null,
    DateTime? TrustedDeviceTokenExpiresAt = null);
