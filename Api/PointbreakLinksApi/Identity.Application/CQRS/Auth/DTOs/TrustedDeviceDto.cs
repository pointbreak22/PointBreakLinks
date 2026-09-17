using Identity.Domain.Entities;

namespace Identity.Application.CQRS.Auth.DTOs;

public record TrustedDeviceDto(int Id, string? Label, string CreatedAt, string ExpiresAt)
{
    public static TrustedDeviceDto FromEntity(TrustedDevice device) => new(
        device.Id,
        device.Label,
        device.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        device.ExpiresAt.ToString("dd.MM.yyyy"));
}
