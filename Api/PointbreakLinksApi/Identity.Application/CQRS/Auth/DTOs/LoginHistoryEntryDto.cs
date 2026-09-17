using Identity.Domain.Entities;

namespace Identity.Application.CQRS.Auth.DTOs;

public record LoginHistoryEntryDto(string? IpAddress, string? UserAgent, string CreatedAt)
{
    public static LoginHistoryEntryDto FromEntity(LoginHistoryEntry entry) => new(
        entry.IpAddress,
        entry.UserAgent,
        entry.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
}
