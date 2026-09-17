// Shape matches Application/CQRS/Messages/DTOs/MessageDto.cs.
export interface MessageDto {
  id: number;
  purchasedSiteId: number;
  senderId: number;
  recipientId: number;
  text: string;
  createdAt: string;
  isRead: boolean;
  // Null unless the message carries a file — see ApiEndpoints.messages.attachment.
  attachmentFileName: string | null;
  // Lets the client render an inline <img> preview for image attachments instead of
  // a generic download button, without guessing from the file extension.
  attachmentContentType: string | null;
}

// Shape matches Application/CQRS/Messages/DTOs/ConversationDto.cs.
export interface ConversationDto {
  purchasedSiteId: number;
  siteUrl: string;
  counterpartyId: number;
  counterpartyName: string;
  lastMessageText: string;
  lastMessageAt: string;
  unreadCount: number;
}
