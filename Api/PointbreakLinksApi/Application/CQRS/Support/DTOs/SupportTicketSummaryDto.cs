namespace Application.CQRS.Support.DTOs;

public record SupportTicketSummaryDto(int Id, string UserName, string LastMessageText, string LastMessageAt, int UnreadCount);
