import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { AdminStore } from '../../../stores/admin.store';
import { ToastService } from '../../../core/notifications/toast.service';
import { extractErrorMessage } from '../../../core/http/api-error';

// Backs a new admin sidebar link — the resolution side of OpenDisputeCommandHandler
// (project-details.html's "Открыть спор" button). Every order currently flagged IsDisputed,
// with two resolve actions: refund the buyer (claws back the seller's payout, see
// ResolveDisputeCommandHandler's comment on why that can push a wallet negative) or release the
// dispute with the order left as-is.
@Component({
  selector: 'app-admin-disputes',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-disputes.html',
})
export class AdminDisputes implements OnInit {
  protected readonly adminStore = inject(AdminStore);
  private readonly toastService = inject(ToastService);

  protected readonly resolvingId = signal<number | null>(null);

  ngOnInit(): void {
    void this.adminStore.fetchDisputes();
  }

  changePage(page: number): void {
    void this.adminStore.fetchDisputes(page);
  }

  async resolve(id: number, refundBuyer: boolean): Promise<void> {
    this.resolvingId.set(id);
    try {
      await this.adminStore.resolveDispute(id, refundBuyer);
      this.toastService.notify(refundBuyer ? 'Спор решён в пользу покупателя' : 'Спор решён в пользу продавца', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось разрешить спор'), 'error');
    } finally {
      this.resolvingId.set(null);
    }
  }
}
