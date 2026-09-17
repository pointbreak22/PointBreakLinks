import { Injectable, inject, signal } from '@angular/core';
import { MessagesApiService } from '../services/messages-api.service';
import { ConversationDto, MessageDto } from '../core/models/message.model';

// Mirrors NotificationsStore's shape: unreadCount is refreshed on login and on every live
// SignalR push (NotificationsBootstrapService), independently of whether the inbox page is
// ever opened; the conversation list and active thread are fetched lazily, only once the
// inbox page (or a conversation within it) is actually opened.
@Injectable({ providedIn: 'root' })
export class MessagesStore {
  private readonly api = inject(MessagesApiService);

  private readonly _conversations = signal<ConversationDto[]>([]);
  private readonly _unreadCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _activeConversation = signal<ConversationDto | null>(null);
  private readonly _thread = signal<MessageDto[]>([]);
  private readonly _threadLoading = signal(false);
  private readonly _sending = signal(false);

  readonly conversations = this._conversations.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly activeConversation = this._activeConversation.asReadonly();
  readonly thread = this._thread.asReadonly();
  readonly threadLoading = this._threadLoading.asReadonly();
  readonly sending = this._sending.asReadonly();

  async refreshUnreadCount(): Promise<void> {
    this._unreadCount.set(await this.api.getUnreadCount());
  }

  async fetchConversations(): Promise<void> {
    this._loading.set(true);
    try {
      this._conversations.set(await this.api.getConversations());
    } finally {
      this._loading.set(false);
    }
  }

  async openConversation(conversation: ConversationDto): Promise<void> {
    this._activeConversation.set(conversation);
    this._threadLoading.set(true);
    try {
      this._thread.set(await this.api.getMessages(conversation.purchasedSiteId));
      // GetMessagesQuery marks the counterparty's messages read server-side as a side effect —
      // mirror that locally so the list's unread badge clears without a round trip.
      this._conversations.update((items) =>
        items.map((c) => (c.purchasedSiteId === conversation.purchasedSiteId ? { ...c, unreadCount: 0 } : c)),
      );
      void this.refreshUnreadCount();
    } finally {
      this._threadLoading.set(false);
    }
  }

  closeConversation(): void {
    this._activeConversation.set(null);
    this._thread.set([]);
  }

  async send(text: string): Promise<void> {
    const conversation = this._activeConversation();
    if (!conversation || this._sending()) return;

    this._sending.set(true);
    try {
      const message = await this.api.sendMessage(conversation.purchasedSiteId, text);
      this._thread.update((current) => [...current, message]);
      this._conversations.update((items) =>
        items.map((c) =>
          c.purchasedSiteId === conversation.purchasedSiteId
            ? { ...c, lastMessageText: message.text, lastMessageAt: message.createdAt }
            : c,
        ),
      );
    } finally {
      this._sending.set(false);
    }
  }

  // Called by NotificationsBootstrapService's shared 'NewMessage' hub handler — re-reads rather
  // than increments locally, same reasoning as NotificationsStore: correct even if this tab
  // missed an earlier push.
  handleIncoming(payload: MessageDto): void {
    const active = this._activeConversation();
    if (active && active.purchasedSiteId === payload.purchasedSiteId) {
      this._thread.update((current) => (current.some((m) => m.id === payload.id) ? current : [...current, payload]));
    }
    void this.refreshUnreadCount();
    void this.fetchConversations();
  }

  clear(): void {
    this._conversations.set([]);
    this._unreadCount.set(0);
    this._activeConversation.set(null);
    this._thread.set([]);
  }
}
