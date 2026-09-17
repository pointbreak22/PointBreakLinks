import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SmartGrid } from '../../../shared/smart-grid/smart-grid';
import { Pagination } from '../../../shared/pagination/pagination';
import { downloadBlob } from '../../../core/http/download-blob';
import { PurchasedSiteDto, getOrderStatusLabel } from '../../../core/models/site.model';
import { SitesApiService } from '../../../services/sites-api.service';
import { WalletApiService } from '../../../services/wallet-api.service';
import { SitesStore } from '../../../stores/sites.store';
import { UserStore } from '../../../stores/user.store';
import { ToastService } from '../../../core/notifications/toast.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { OrderTaskModal } from '../order-task-modal/order-task-modal';
import { MessageChatModal } from '../../../shared/message-chat-modal/message-chat-modal';

// Ported from FOXLinks' page-components/webmaster/my-sales.vue.
@Component({
  selector: 'app-my-sales',
  imports: [DecimalPipe, SmartGrid, Pagination, OrderTaskModal, MessageChatModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-sales.html',
})
export class MySales implements OnInit {
  protected readonly sitesStore = inject(SitesStore);
  private readonly sitesApi = inject(SitesApiService);
  private readonly walletApi = inject(WalletApiService);
  private readonly userStore = inject(UserStore);
  private readonly toastService = inject(ToastService);

  protected readonly taskModalOrder = signal<PurchasedSiteDto | null>(null);
  protected readonly chatModalOrder = signal<PurchasedSiteDto | null>(null);
  protected readonly exporting = signal(false);

  ngOnInit(): void {
    void this.sitesStore.fetchSales();
  }

  changePage(page: number): void {
    void this.sitesStore.fetchSales(page);
  }

  protected readonly getOrderStatusLabel = getOrderStatusLabel;

  truncate(text: string | null, length: number): string {
    if (!text) return '';
    return text.length > length ? text.slice(0, length) + '...' : text;
  }

  openTask(order: PurchasedSiteDto): void {
    this.taskModalOrder.set(order);
  }

  closeTask(): void {
    this.taskModalOrder.set(null);
  }

  openChat(order: PurchasedSiteDto): void {
    this.chatModalOrder.set(order);
  }

  closeChat(): void {
    this.chatModalOrder.set(null);
  }

  async acceptOrder(id: number): Promise<void> {
    try {
      await this.sitesStore.acceptOrder(id);
      this.toastService.notify('Заказ принят в работу', 'success');
      this.userStore.setBalance(await this.walletApi.getBalance());
    } finally {
      this.closeTask();
    }
  }

  async rejectOrder(id: number): Promise<void> {
    try {
      await this.sitesStore.declineOrder(id);
      this.toastService.notify('Заказ отклонён, средства возвращены покупателю', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отклонить заказ'), 'error');
    } finally {
      this.closeTask();
    }
  }

  async confirmPublish(id: number): Promise<void> {
    try {
      await this.sitesStore.confirmPublished(id);
      this.toastService.notify('Публикация подтверждена', 'success');
    } finally {
      this.closeTask();
    }
  }

  async exportCsv(): Promise<void> {
    this.exporting.set(true);
    try {
      downloadBlob(await this.sitesApi.exportWebmasterSales(), 'sales.csv');
    } catch {
      this.toastService.notify('Не удалось экспортировать данные', 'error');
    } finally {
      this.exporting.set(false);
    }
  }
}
