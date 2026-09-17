namespace Identity.Application.CQRS.Auth.DTOs;

// Email+password alone is never enough to know the outcome once 2FA exists — either it issues
// real tokens (Auth set) or it hands back a short-lived ticket the client must pair with a TOTP
// code via CompleteTwoFactorLoginCommand (RequiresTwoFactor true, Auth null). Exactly one of
// TwoFactorTicket/Auth is ever set.
public record LoginResultDto(bool RequiresTwoFactor, string? TwoFactorTicket, AuthResultDto? Auth)
{
    public static LoginResultDto Success(AuthResultDto auth) => new(false, null, auth);

    public static LoginResultDto NeedsTwoFactor(string ticket) => new(true, ticket, null);
}
