import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Header } from '../../shared/layout/header/header';
import { SupportStore } from '../../stores/support.store';
import { UserStore } from '../../stores/user.store';
import { ToastService } from '../../core/notifications/toast.service';

// Backs the sidebar's "Обратная связь" link (previously a dead coming-soon stub). One
// persistent thread with "the support team" — see SupportStore's comment.
@Component({
  selector: 'app-support',
  imports: [Header, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './support.html',
})
export class Support implements OnInit {
  protected readonly store = inject(SupportStore);
  protected readonly userStore = inject(UserStore);
  private readonly toastService = inject(ToastService);

  protected draftText = '';

  ngOnInit(): void {
    void this.store.fetchMyThread();
  }

  async send(): Promise<void> {
    const text = this.draftText.trim();
    if (!text) return;

    try {
      await this.store.sendMyMessage(text);
      this.draftText = '';
    } catch {
      this.toastService.notify('Не удалось отправить сообщение', 'error');
    }
  }
}
