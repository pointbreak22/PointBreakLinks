import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NotificationHubService } from '../../core/signalr/notification-hub.service';
import { MessageDto } from '../../core/models/message.model';
import { PurchasedSiteDto } from '../../core/models/site.model';
import { MessagesApiService } from '../../services/messages-api.service';
import { downloadBlob } from '../../core/http/download-blob';
import { ToastService } from '../../core/notifications/toast.service';
import { extractErrorMessage } from '../../core/http/api-error';
import { UserStore } from '../../stores/user.store';

// Kept in sync with MessagesController's own allowlist server-side — this is only a UX
// shortcut (fewer round trips for an obviously-wrong file), never the actual security boundary.
const ALLOWED_ATTACHMENT_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.pdf', '.txt', '.doc', '.docx', '.xls', '.xlsx', '.zip'];
const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

// Payload SignalRNotificationPusher.NotifyNewMessageAsync sends — a MessageDto plus senderName,
// the latter used only by NotificationsBootstrapService's toast, ignored here.
type NewMessagePush = MessageDto & { senderName: string };

// FOXLinks' modal-windows/webmaster/message-chat-modal.vue is entirely hardcoded fake
// conversation data — no send handler, no fetch, a name that's never filled in. This is a real
// implementation instead, backed by WebAPI/Controllers/MessagesController.cs (built alongside
// this — the Message entity already existed from the initial scaffold, so wiring it up for
// real cost little more than faking it would have).
@Component({
  selector: 'app-message-chat-modal',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './message-chat-modal.html',
})
export class MessageChatModal implements OnInit, OnDestroy {
  private readonly messagesApi = inject(MessagesApiService);
  private readonly toastService = inject(ToastService);
  private readonly hub = inject(NotificationHubService);
  protected readonly userStore = inject(UserStore);

  readonly order = input.required<PurchasedSiteDto>();
  readonly close = output<void>();

  protected readonly messages = signal<MessageDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly sending = signal(false);
  protected readonly downloadingId = signal<number | null>(null);
  protected draftText = '';

  // Object URLs for image attachments, keyed by message id, so an <img> can render them inline
  // instead of the generic download button — attachments stay [Authorize]-gated server-side, so
  // a plain <img src> can't carry the Bearer token; this fetches the blob once and caches it.
  protected readonly imagePreviews = signal<Map<number, string>>(new Map());

  // Bound once so the exact same function reference can be passed to hub.off() on destroy —
  // an inline arrow function in ngOnInit would unsubscribe nothing (off() matches by reference).
  private readonly onNewMessage = (payload: NewMessagePush): void => {
    if (payload.purchasedSiteId !== this.order().id) return;
    this.messages.update((current) => (current.some((m) => m.id === payload.id) ? current : [...current, payload]));
    void this.loadImagePreview(payload);
  };

  ngOnInit(): void {
    void this.loadMessages();
    this.hub.on<NewMessagePush>('NewMessage', this.onNewMessage);
  }

  ngOnDestroy(): void {
    this.hub.off<NewMessagePush>('NewMessage', this.onNewMessage);
    for (const url of this.imagePreviews().values()) {
      URL.revokeObjectURL(url);
    }
  }

  private async loadMessages(): Promise<void> {
    this.loading.set(true);
    try {
      const messages = await this.messagesApi.getMessages(this.order().id);
      this.messages.set(messages);
      for (const message of messages) {
        void this.loadImagePreview(message);
      }
    } catch {
      this.toastService.notify('Не удалось загрузить сообщения', 'error');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadImagePreview(message: MessageDto): Promise<void> {
    if (!message.attachmentContentType?.startsWith('image/') || this.imagePreviews().has(message.id)) return;

    try {
      const blob = await this.messagesApi.downloadAttachment(this.order().id, message.id);
      const url = URL.createObjectURL(blob);
      this.imagePreviews.update((current) => new Map(current).set(message.id, url));
    } catch {
      // Silently fall back to the generic download button — a missing/failed preview isn't
      // worth a toast when the file can still be downloaded normally.
    }
  }

  async send(): Promise<void> {
    const text = this.draftText.trim();
    if (!text || this.sending()) return;

    this.sending.set(true);
    try {
      const message = await this.messagesApi.sendMessage(this.order().id, text);
      this.messages.update((current) => [...current, message]);
      this.draftText = '';
    } catch {
      this.toastService.notify('Не удалось отправить сообщение', 'error');
    } finally {
      this.sending.set(false);
    }
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // lets picking the same file twice in a row still fire a change event
    if (!file || this.sending()) return;

    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!ALLOWED_ATTACHMENT_EXTENSIONS.includes(extension)) {
      this.toastService.notify('Недопустимый тип файла', 'error');
      return;
    }
    if (file.size > MAX_ATTACHMENT_BYTES) {
      this.toastService.notify('Файл слишком большой — максимум 10 МБ', 'error');
      return;
    }

    this.sending.set(true);
    try {
      const message = await this.messagesApi.sendAttachment(this.order().id, this.draftText.trim(), file);
      this.messages.update((current) => [...current, message]);
      this.draftText = '';
      void this.loadImagePreview(message);
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отправить файл'), 'error');
    } finally {
      this.sending.set(false);
    }
  }

  async downloadAttachment(message: MessageDto): Promise<void> {
    if (!message.attachmentFileName || this.downloadingId() === message.id) return;

    this.downloadingId.set(message.id);
    try {
      const blob = await this.messagesApi.downloadAttachment(this.order().id, message.id);
      downloadBlob(blob, message.attachmentFileName);
    } catch {
      this.toastService.notify('Не удалось скачать файл', 'error');
    } finally {
      this.downloadingId.set(null);
    }
  }
}
