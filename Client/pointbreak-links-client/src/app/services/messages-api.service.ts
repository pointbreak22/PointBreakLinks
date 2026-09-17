import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { ConversationDto, MessageDto } from '../core/models/message.model';

@Injectable({ providedIn: 'root' })
export class MessagesApiService {
  private readonly http = inject(HttpClient);

  getMessages(purchasedSiteId: number): Promise<MessageDto[]> {
    return firstValueFrom(this.http.get<MessageDto[]>(ApiEndpoints.messages.byPurchasedSite(purchasedSiteId)));
  }

  sendMessage(purchasedSiteId: number, text: string): Promise<MessageDto> {
    return firstValueFrom(this.http.post<MessageDto>(ApiEndpoints.messages.byPurchasedSite(purchasedSiteId), { text }));
  }

  sendAttachment(purchasedSiteId: number, text: string, file: File): Promise<MessageDto> {
    const form = new FormData();
    form.append('text', text);
    form.append('file', file);
    return firstValueFrom(this.http.post<MessageDto>(ApiEndpoints.messages.attachmentUpload(purchasedSiteId), form));
  }

  downloadAttachment(purchasedSiteId: number, messageId: number): Promise<Blob> {
    return firstValueFrom(
      this.http.get(ApiEndpoints.messages.attachmentDownload(purchasedSiteId, messageId), { responseType: 'blob' }),
    );
  }

  getConversations(): Promise<ConversationDto[]> {
    return firstValueFrom(this.http.get<ConversationDto[]>(ApiEndpoints.conversations.mine));
  }

  getUnreadCount(): Promise<number> {
    return firstValueFrom(this.http.get<number>(ApiEndpoints.conversations.unreadCount));
  }
}
