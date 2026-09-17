using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.CompleteTwoFactorLogin;

// DeviceLabel is a best-effort User-Agent string captured by the controller — stored on the new
// TrustedDevice row when RememberDevice is set, and doubles as this login's UserAgent in
// "История входов" either way. IpAddress is likewise only ever used for that history list.
public record CompleteTwoFactorLoginCommand(
    string Ticket,
    string Code,
    bool RememberDevice,
    string? DeviceLabel,
    string? IpAddress) : IRequest<AuthResultDto>;
