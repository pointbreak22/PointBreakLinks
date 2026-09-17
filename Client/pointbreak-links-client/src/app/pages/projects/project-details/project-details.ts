import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SidebarService } from '../../../core/layout/sidebar.service';
import { downloadBlob } from '../../../core/http/download-blob';
import { extractErrorMessage } from '../../../core/http/api-error';
import { PurchasedSiteDto, getOrderStatusLabel } from '../../../core/models/site.model';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { SmartGrid } from '../../../shared/smart-grid/smart-grid';
import { Pagination } from '../../../shared/pagination/pagination';
import { MessageChatModal } from '../../../shared/message-chat-modal/message-chat-modal';
import { PurchasedSiteTimeline } from '../../../shared/purchased-site-timeline/purchased-site-timeline';
import { SitesApiService } from '../../../services/sites-api.service';
import { SitesStore } from '../../../stores/sites.store';
import { ToastService } from '../../../core/notifications/toast.service';
import { LeaveReviewModal } from '../leave-review-modal/leave-review-modal';

// Ported from FOXLinks' pages/project/project-details.vue. Same dead toolbar
// ("Добавить ссылку"/"Экспорт в *.xls") skipped as in project-list. The chat icon in the
// source just does `console.log(...)` — here it opens the real MessageChatModal (shared with
// Webmaster) instead.
@Component({
  selector: 'app-project-details',
  imports: [RouterLink, Header, Sidebar, SmartGrid, Pagination, MessageChatModal, LeaveReviewModal, PurchasedSiteTimeline],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-details.html',
})
export class ProjectDetails implements OnInit {
  protected readonly sidebarService = inject(SidebarService);
  protected readonly sitesStore = inject(SitesStore);
  protected readonly getOrderStatusLabel = getOrderStatusLabel;
  private readonly sitesApi = inject(SitesApiService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly projectName = signal('Без названия');
  protected readonly chatModalOrder = signal<PurchasedSiteDto | null>(null);
  protected readonly reviewModalOrder = signal<PurchasedSiteDto | null>(null);
  protected readonly expandedOrderId = signal<number | null>(null);
  protected readonly exporting = signal(false);
  protected readonly cancellingId = signal<number | null>(null);
  protected readonly disputingOrderId = signal<number | null>(null);
  protected disputeReason = '';
  protected readonly submittingDispute = signal(false);

  private projectId = 0;

  ngOnInit(): void {
    this.projectId = Number(this.route.snapshot.paramMap.get('id'));
    this.projectName.set(this.route.snapshot.queryParamMap.get('name') ?? 'Без названия');

    if (!this.projectId) {
      void this.router.navigateByUrl('/projects');
      return;
    }
    void this.sitesStore.fetchSitesByProject(this.projectId);
  }

  changePage(page: number): void {
    void this.sitesStore.fetchSitesByProject(this.projectId, page);
  }

  openChat(order: PurchasedSiteDto): void {
    this.chatModalOrder.set(order);
  }

  closeChat(): void {
    this.chatModalOrder.set(null);
  }

  toggleTimeline(orderId: number): void {
    this.expandedOrderId.update((current) => (current === orderId ? null : orderId));
  }

  openReview(order: PurchasedSiteDto): void {
    this.reviewModalOrder.set(order);
  }

  closeReview(): void {
    this.reviewModalOrder.set(null);
  }

  onReviewSubmitted(): void {
    void this.sitesStore.fetchSitesByProject(this.projectId, this.sitesStore.meta().currentPage);
  }

  async cancelOrder(order: PurchasedSiteDto): Promise<void> {
    this.cancellingId.set(order.id);
    try {
      await this.sitesStore.cancelOrder(order.id);
      this.toastService.notify('Заказ отменён, средства возвращены на баланс', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отменить заказ'), 'error');
    } finally {
      this.cancellingId.set(null);
    }
  }

  toggleDispute(orderId: number): void {
    this.disputingOrderId.update((current) => (current === orderId ? null : orderId));
    this.disputeReason = '';
  }

  onDisputeReasonInput(event: Event): void {
    this.disputeReason = (event.target as HTMLTextAreaElement).value;
  }

  async submitDispute(order: PurchasedSiteDto): Promise<void> {
    const reason = this.disputeReason.trim();
    if (!reason || this.submittingDispute()) return;

    this.submittingDispute.set(true);
    try {
      await this.sitesStore.openDispute(order.id, reason);
      this.toastService.notify('Спор открыт, администратор рассмотрит заказ', 'success');
      this.disputingOrderId.set(null);
      this.disputeReason = '';
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось открыть спор'), 'error');
    } finally {
      this.submittingDispute.set(false);
    }
  }

  async exportCsv(): Promise<void> {
    this.exporting.set(true);
    try {
      downloadBlob(await this.sitesApi.exportProjectSites(this.projectId), 'project-sites.csv');
    } catch {
      this.toastService.notify('Не удалось экспортировать данные', 'error');
    } finally {
      this.exporting.set(false);
    }
  }
}
