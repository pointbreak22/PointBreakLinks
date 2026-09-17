using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Login;

// TrustedDeviceToken comes from the "trusted_device" cookie, if any — see
// CompleteTwoFactorLoginCommand's comment for how it's minted. When it matches an unexpired
// TrustedDevice row for this user, LoginCommandHandler skips the 2FA step entirely.
// IpAddress/UserAgent are captured by the controller purely for the "История входов" list —
// never used for any security decision here (unlike TrustedDeviceToken).
public record LoginCommand(
    string Email,
    string Password,
    string? TrustedDeviceToken = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<LoginResultDto>;
