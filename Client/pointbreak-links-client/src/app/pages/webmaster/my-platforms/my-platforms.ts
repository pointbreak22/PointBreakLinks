import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SmartGrid } from '../../../shared/smart-grid/smart-grid';
import { Pagination } from '../../../shared/pagination/pagination';
import { extractErrorMessage } from '../../../core/http/api-error';
import { ToastService } from '../../../core/notifications/toast.service';
import { SitesStore } from '../../../stores/sites.store';
import { UserStore } from '../../../stores/user.store';
import { AddEditSiteModal } from '../add-edit-site-modal/add-edit-site-modal';
import { SiteReviewsModal } from '../../../shared/site-reviews-modal/site-reviews-modal';
import { SiteDto } from '../../../core/models/site.model';
import { ApiEndpoints } from '../../../core/http/api-endpoints';

// Ported from FOXLinks' page-components/webmaster/my-platforms.vue. Ownership verification
// (VerificationToken/verify()) is new, not from FOXLinks.
@Component({
  selector: 'app-my-platforms',
  imports: [DecimalPipe, SmartGrid, Pagination, AddEditSiteModal, SiteReviewsModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-platforms.html',
})
export class MyPlatforms implements OnInit {
  protected readonly sitesStore = inject(SitesStore);
  protected readonly userStore = inject(UserStore);
  private readonly toastService = inject(ToastService);

  protected readonly modalOpen = signal(false);
  protected readonly editingSiteId = signal<number | null>(null);
  protected readonly verifyingId = signal<number | null>(null);
  protected readonly instructionsSiteId = signal<number | null>(null);
  protected readonly reviewsSite = signal<SiteDto | null>(null);
  protected readonly screenshotUrl = (id: number) => ApiEndpoints.sites.screenshot(id);
  protected readonly pausingId = signal<number | null>(null);

  ngOnInit(): void {
    void this.sitesStore.fetchMySites();
  }

  changePage(page: number): void {
    void this.sitesStore.fetchMySites(page);
  }

  openCreate(): void {
    this.editingSiteId.set(null);
    this.modalOpen.set(true);
  }

  openEdit(id: number): void {
    this.editingSiteId.set(id);
    this.modalOpen.set(true);
  }

  onSaved(): void {
    void this.sitesStore.fetchMySites();
  }

  toggleInstructions(siteId: number): void {
    this.instructionsSiteId.update((current) => (current === siteId ? null : siteId));
  }

  openReviews(site: SiteDto): void {
    this.reviewsSite.set(site);
  }

  closeReviews(): void {
    this.reviewsSite.set(null);
  }

  async verify(siteId: number): Promise<void> {
    this.verifyingId.set(siteId);
    try {
      const verified = await this.sitesStore.verifySite(siteId);
      this.toastService.notify(
        verified ? 'Владение площадкой подтверждено' : 'Код подтверждения не найден на странице',
        verified ? 'success' : 'error',
      );
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось проверить площадку'), 'error');
    } finally {
      this.verifyingId.set(null);
    }
  }

  // Pause/resume a listing — a reversible IsActive flip (DeactivateSite/ReactivateSiteCommand),
  // not a delete, so order history/chat/reviews are untouched and the URL/price/etc. stay saved
  // for whenever the seller turns it back on.
  async togglePause(site: SiteDto): Promise<void> {
    this.pausingId.set(site.id);
    try {
      if (site.isActive) {
        await this.sitesStore.deactivateSite(site.id);
        this.toastService.notify('Площадка приостановлена и скрыта из каталога', 'success');
      } else {
        await this.sitesStore.reactivateSite(site.id);
        this.toastService.notify('Площадка снова видна в каталоге', 'success');
      }
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось изменить статус площадки'), 'error');
    } finally {
      this.pausingId.set(null);
    }
  }
}
