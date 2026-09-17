import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { SupportStore } from '../../../stores/support.store';
import { ToastService } from '../../../core/notifications/toast.service';
import { SupportTicketSummaryDto } from '../../../core/models/support.model';

// Staff side of the support feature — every ticket platform-wide, any admin/moderator can open
// and reply to any of them. Backs a new admin sidebar link (there was nothing here before —
// the "Обратная связь" flow only ever existed as a dead stub for regular users).
@Component({
  selector: 'app-admin-support',
  imports: [Header, Sidebar, FormsModule, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-support.html',
})
export class AdminSupport implements OnInit {
  protected readonly store = inject(SupportStore);
  private readonly toastService = inject(ToastService);

  protected draftText = '';

  ngOnInit(): void {
    void this.store.fetchTickets();
  }

  changePage(page: number): void {
    void this.store.fetchTickets(page);
  }

  async open(ticket: SupportTicketSummaryDto): Promise<void> {
    try {
      await this.store.openTicket(ticket);
    } catch {
      this.toastService.notify('Не удалось загрузить обращение', 'error');
    }
  }

  back(): void {
    this.store.closeTicket();
  }

  async send(): Promise<void> {
    const text = this.draftText.trim();
    if (!text) return;

    try {
      await this.store.sendReply(text);
      this.draftText = '';
    } catch {
      this.toastService.notify('Не удалось отправить ответ', 'error');
    }
  }
}
