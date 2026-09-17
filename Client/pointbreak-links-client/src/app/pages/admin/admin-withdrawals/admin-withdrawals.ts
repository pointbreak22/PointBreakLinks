import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { AdminStore } from '../../../stores/admin.store';
import { AdminApiService } from '../../../services/admin-api.service';
import { ToastService } from '../../../core/notifications/toast.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { downloadBlob } from '../../../core/http/download-blob';

// Admin side of RequestWithdrawalCommandHandler's honest gap: no real payout provider exists,
// so approving here just records that the admin sent the money manually outside the app.
// Rejecting refunds the user's wallet (see RejectWithdrawalCommandHandler).
@Component({
  selector: 'app-admin-withdrawals',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-withdrawals.html',
})
export class AdminWithdrawals implements OnInit {
  protected readonly adminStore = inject(AdminStore);
  private readonly adminApi = inject(AdminApiService);
  private readonly toastService = inject(ToastService);

  protected readonly processingId = signal<number | null>(null);
  protected readonly rejectingId = signal<number | null>(null);
  protected readonly exporting = signal(false);
  protected rejectComment = '';

  ngOnInit(): void {
    void this.adminStore.fetchWithdrawals();
  }

  changePage(page: number): void {
    void this.adminStore.fetchWithdrawals(page);
  }

  // Full history (every status, not just the pending queue shown above) — same "all" export
  // scope as admin-reports' orders/transactions/users CSVs.
  async exportCsv(): Promise<void> {
    this.exporting.set(true);
    try {
      const blob = await this.adminApi.exportWithdrawals();
      downloadBlob(blob, 'withdrawals.csv');
    } catch {
      this.toastService.notify('Не удалось экспортировать данные', 'error');
    } finally {
      this.exporting.set(false);
    }
  }

  async approve(id: number): Promise<void> {
    this.processingId.set(id);
    try {
      await this.adminStore.approveWithdrawal(id);
      this.toastService.notify('Заявка одобрена', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось одобрить заявку'), 'error');
    } finally {
      this.processingId.set(null);
    }
  }

  toggleReject(id: number): void {
    this.rejectingId.update((current) => (current === id ? null : id));
    this.rejectComment = '';
  }

  onRejectCommentInput(event: Event): void {
    this.rejectComment = (event.target as HTMLTextAreaElement).value;
  }

  async confirmReject(id: number): Promise<void> {
    this.processingId.set(id);
    try {
      await this.adminStore.rejectWithdrawal(id, this.rejectComment.trim());
      this.toastService.notify('Заявка отклонена, средства возвращены', 'success');
      this.rejectingId.set(null);
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отклонить заявку'), 'error');
    } finally {
      this.processingId.set(null);
    }
  }
}
