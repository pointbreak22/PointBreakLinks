import { ChangeDetectionStrategy, Component, PLATFORM_ID, computed, inject, input, output, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { PurchasedSiteDto } from '../../../core/models/site.model';
import { ToastService } from '../../../core/notifications/toast.service';
import { PurchasedSiteTimeline } from '../../../shared/purchased-site-timeline/purchased-site-timeline';

// Ported from FOXLinks' modal-windows/webmaster/order-task-modal.vue. "Подтвердить публикацию"
// is new — the backend endpoint (POST /sites/{id}/confirm-published) already existed from an
// earlier session, but nothing in the client ever called it, so a purchased site's IsPublished
// flag could never actually become true through the UI. Added once that turned out to block the
// review feature (a buyer can only review a placement once it's confirmed published).
@Component({
  selector: 'app-order-task-modal',
  imports: [PurchasedSiteTimeline],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-task-modal.html',
})
export class OrderTaskModal {
  private readonly toastService = inject(ToastService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly order = input.required<PurchasedSiteDto>();
  readonly close = output<void>();
  readonly accept = output<number>();
  readonly reject = output<number>();
  readonly confirmPublish = output<number>();

  protected readonly submitting = signal(false);

  protected readonly articleStatus = computed(() =>
    this.order().isPublished ? 'Статья опубликована' : 'Статья ещё не написана',
  );

  protected readonly statusIcon = computed(() => (this.order().isPublished ? 'fa-check-circle' : 'fa-clock'));

  // "application" — still awaiting the seller's accept/reject decision.
  protected readonly isPending = computed(() => this.order().status.name === 'application');
  // "work" — accepted, not yet confirmed published.
  protected readonly isAcceptedNotPublished = computed(
    () => this.order().status.name === 'work' && !this.order().isPublished,
  );

  async copyToClipboard(text: string): Promise<void> {
    if (!this.isBrowser) return;
    await navigator.clipboard.writeText(text);
    this.toastService.notify('Ссылка скопирована!', 'success');
  }

  async copyAllLinks(): Promise<void> {
    if (!this.isBrowser) return;
    const all = this.order()
      .links.map((l) => l.url)
      .join('\n');
    await navigator.clipboard.writeText(all);
    this.toastService.notify('Все ссылки скопированы!', 'success');
  }

  async handleAccept(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    try {
      this.accept.emit(this.order().id);
    } finally {
      this.submitting.set(false);
    }
  }

  async handleConfirmPublish(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    try {
      this.confirmPublish.emit(this.order().id);
    } finally {
      this.submitting.set(false);
    }
  }
}
