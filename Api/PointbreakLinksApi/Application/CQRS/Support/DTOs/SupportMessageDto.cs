using Domain.Entities;

namespace Application.CQRS.Support.DTOs;

public record SupportMessageDto(int Id, int SupportTicketId, int SenderId, string SenderName, bool IsFromStaff, string Text, string CreatedAt, bool IsRead)
{
    public static SupportMessageDto FromEntity(SupportMessage message) => new(
        message.Id,
        message.SupportTicketId,
        message.SenderId,
        message.Sender.Name,
        message.IsFromStaff,
        message.Text,
        message.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        message.ReadAt != null);
}
