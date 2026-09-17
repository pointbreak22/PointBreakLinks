namespace Identity.Application.Common;

// TOTP = Time-based One-Time Password (RFC 6238), the algorithm behind Google Authenticator/
// Authy/1Password etc. — no external account or provider involved, just a shared secret and a
// clock, so there's nothing to "swap for the real thing later" the way SmtpEmailSender or
// EmailSettings are — this one already is the real thing.
public interface ITotpService
{
    string GenerateSecret();
    string BuildOtpAuthUri(string secret, string accountEmail, string issuer);
    bool ValidateCode(string secret, string code);
}
