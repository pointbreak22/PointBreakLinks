namespace Identity.Application.CQRS.Auth.DTOs;

// Secret shown both raw (manual entry) and as an otpauth:// URI (QR code, rendered client-side)
// — standard pairing shown by every authenticator app's own setup screen.
public record TwoFactorSetupDto(string Secret, string OtpAuthUri);
