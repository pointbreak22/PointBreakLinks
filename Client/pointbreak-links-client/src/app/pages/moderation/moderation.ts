import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../shared/pagination/pagination';
import { extractErrorMessage } from '../../core/http/api-error';
import { ToastService } from '../../core/notifications/toast.service';
import { ModerationStore } from '../../stores/moderation.store';

// New module — the `moderator` role existed in FOXLinks' seeded roles table but had no page
// anywhere in the source. Real gap: a Site is created inactive pending review (see
// CreateSiteCommandHandler) but nothing ever reviewed it before this.
@Component({
  selector: 'app-moderation',
  imports: [Header, Sidebar, Pagination, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './moderation.html',
})
export class Moderation implements OnInit {
  protected readonly moderationStore = inject(ModerationStore);
  private readonly toastService = inject(ToastService);

  ngOnInit(): void {
    void this.moderationStore.fetchPendingSites();
  }

  changePage(page: number): void {
    void this.moderationStore.fetchPendingSites(page);
  }

  async approve(id: number): Promise<void> {
    try {
      await this.moderationStore.approveSite(id);
      this.toastService.notify('Площадка одобрена', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось одобрить площадку'), 'error');
    }
  }

  // "bulk" for the bulk-action bar's reject, a site id for a single row's — either way, only one
  // reason box is open at a time, since a moderator working through the queue rejects one thing
  // (or one batch) before moving to the next.
  protected readonly rejectingId = signal<number | 'bulk' | null>(null);
  protected readonly rejectReason = signal('');

  startReject(id: number): void {
    this.rejectingId.set(id);
    this.rejectReason.set('');
  }

  startBulkReject(): void {
    this.rejectingId.set('bulk');
    this.rejectReason.set('');
  }

  cancelReject(): void {
    this.rejectingId.set(null);
    this.rejectReason.set('');
  }

  async confirmReject(): Promise<void> {
    const target = this.rejectingId();
    if (target === null) return;

    const reason = this.rejectReason().trim() || undefined;
    try {
      if (target === 'bulk') {
        const count = this.moderationStore.selectedIds().size;
        await this.moderationStore.bulkRejectSelected(reason);
        this.toastService.notify(`Отклонено площадок: ${count}`, 'success');
      } else {
        await this.moderationStore.rejectSite(target, reason);
        this.toastService.notify('Площадка отклонена', 'success');
      }
      this.cancelReject();
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отклонить площадку'), 'error');
    }
  }

  toggleSelected(id: number): void {
    this.moderationStore.toggleSelected(id);
  }

  toggleSelectAll(): void {
    this.moderationStore.toggleSelectAll();
  }

  async bulkApprove(): Promise<void> {
    const count = this.moderationStore.selectedIds().size;
    try {
      await this.moderationStore.bulkApproveSelected();
      this.toastService.notify(`Одобрено площадок: ${count}`, 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось одобрить выбранные площадки'), 'error');
    }
  }

}
