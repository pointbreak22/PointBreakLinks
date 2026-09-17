// Shape matches Application/CQRS/Support/DTOs/SupportMessageDto.cs.
export interface SupportMessageDto {
  id: number;
  supportTicketId: number;
  senderId: number;
  senderName: string;
  isFromStaff: boolean;
  text: string;
  createdAt: string;
  isRead: boolean;
}

// Shape matches Application/CQRS/Support/DTOs/SupportTicketSummaryDto.cs.
export interface SupportTicketSummaryDto {
  id: number;
  userName: string;
  lastMessageText: string;
  lastMessageAt: string;
  unreadCount: number;
}
