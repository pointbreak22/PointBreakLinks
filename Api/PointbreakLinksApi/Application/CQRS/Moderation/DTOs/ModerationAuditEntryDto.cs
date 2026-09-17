using Domain.Entities;

namespace Application.CQRS.Moderation.DTOs;

public record ModerationAuditEntryDto(
    string SiteUrl,
    string ModeratorName,
    string Action,
    string? Reason,
    string CreatedAt)
{
    // Requires Site and Moderator loaded (see IModerationAuditRepository.GetPagedAsync).
    public static ModerationAuditEntryDto FromEntity(ModerationAuditEntry entry) => new(
        entry.Site.Url,
        entry.Moderator.Name,
        entry.Action,
        entry.Reason,
        entry.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
}
