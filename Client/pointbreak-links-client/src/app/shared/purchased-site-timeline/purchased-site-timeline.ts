import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { PurchasedSiteEventDto } from '../../core/models/site.model';
import { SitesApiService } from '../../services/sites-api.service';

// Reusable order-history strip, backed by PurchasedSiteEvent (Domain/Entities/PurchasedSiteEvent.cs)
// — events are written by RequestPublicationCommandHandler, AcceptOrderCommandHandler,
// ConfirmPublishedCommandHandler and CreateReviewCommandHandler as each real state change
// happens, not derived from PurchasedSite.StatusId alone (publication confirmation doesn't
// change StatusId at all, so a status-only timeline would miss it). Used by both sides of an
// order: order-task-modal (seller) and project-details (buyer).
@Component({
  selector: 'app-purchased-site-timeline',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './purchased-site-timeline.html',
})
export class PurchasedSiteTimeline {
  private readonly sitesApi = inject(SitesApiService);

  readonly purchasedSiteId = input.required<number>();

  protected readonly events = signal<PurchasedSiteEventDto[]>([]);
  protected readonly loading = signal(false);

  constructor() {
    effect(() => {
      void this.loadEvents(this.purchasedSiteId());
    });
  }

  private async loadEvents(purchasedSiteId: number): Promise<void> {
    this.loading.set(true);
    try {
      this.events.set(await this.sitesApi.getPurchasedSiteEvents(purchasedSiteId));
    } finally {
      this.loading.set(false);
    }
  }
}
