import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Header } from '../../shared/layout/header/header';
import { MessagesStore } from '../../stores/messages.store';
import { ToastService } from '../../core/notifications/toast.service';
import { UserStore } from '../../stores/user.store';
import { ConversationDto } from '../../core/models/message.model';

// New page — aggregates every order-chat (previously only reachable one order at a time via
// app-message-chat-modal from project-details/my-sales) into a single inbox, and gives the
// header's "Сообщения" icon — a dead placeholder button until now — somewhere to go.
@Component({
  selector: 'app-messages',
  imports: [Header, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './messages.html',
})
export class Messages implements OnInit {
  protected readonly store = inject(MessagesStore);
  protected readonly userStore = inject(UserStore);
  private readonly toastService = inject(ToastService);

  protected draftText = '';

  ngOnInit(): void {
    void this.store.fetchConversations();
  }

  async open(conversation: ConversationDto): Promise<void> {
    try {
      await this.store.openConversation(conversation);
    } catch {
      this.toastService.notify('Не удалось загрузить переписку', 'error');
    }
  }

  back(): void {
    this.store.closeConversation();
  }

  async send(): Promise<void> {
    const text = this.draftText.trim();
    if (!text) return;

    try {
      await this.store.send(text);
      this.draftText = '';
    } catch {
      this.toastService.notify('Не удалось отправить сообщение', 'error');
    }
  }
}
