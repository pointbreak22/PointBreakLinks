import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { ReviewsApiService } from '../../services/reviews-api.service';
import { ToastService } from '../../core/notifications/toast.service';
import { SiteReviewDto } from '../../core/models/site.model';
import { PageMeta } from '../../core/models/pagination.model';
import { Pagination } from '../pagination/pagination';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 10, total: 0 };

// New component, not a port — GET api/sites/{id}/reviews and ReviewsApiService.getSiteReviews
// already existed and worked, but nothing in the UI ever called them: the star+count badge in
// optimizator.html/my-platforms.html showed the aggregate, with no way to read what anyone
// actually wrote. This modal is that missing reader, opened by clicking the badge.
@Component({
  selector: 'app-site-reviews-modal',
  imports: [Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './site-reviews-modal.html',
})
export class SiteReviewsModal implements OnInit {
  private readonly reviewsApi = inject(ReviewsApiService);
  private readonly toastService = inject(ToastService);

  readonly siteId = input.required<number>();
  readonly siteUrl = input.required<string>();
  // Only true from my-platforms (the seller's own listings page) — optimizator.html shows this
  // same modal to buyers, who must never see a reply control.
  readonly canReply = input(false);
  readonly close = output<void>();

  protected readonly reviews = signal<SiteReviewDto[]>([]);
  protected readonly meta = signal<PageMeta>(emptyMeta);
  protected readonly loading = signal(false);
  protected readonly stars = [1, 2, 3, 4, 5];

  protected readonly replyingId = signal<number | null>(null);
  protected readonly replyText = signal('');
  protected readonly replyBusy = signal(false);

  ngOnInit(): void {
    void this.load();
  }

  async load(page = 1): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.reviewsApi.getSiteReviews(this.siteId(), page);
      this.reviews.set(response.items);
      this.meta.set(response);
    } catch {
      this.toastService.notify('Не удалось загрузить отзывы', 'error');
    } finally {
      this.loading.set(false);
    }
  }

  startReply(review: SiteReviewDto): void {
    this.replyingId.set(review.id);
    this.replyText.set(review.sellerReply ?? '');
  }

  cancelReply(): void {
    this.replyingId.set(null);
    this.replyText.set('');
  }

  async submitReply(reviewId: number): Promise<void> {
    const reply = this.replyText().trim();
    if (!reply) return;

    this.replyBusy.set(true);
    try {
      const updated = await this.reviewsApi.replyToReview(reviewId, reply);
      this.reviews.update((reviews) => reviews.map((r) => (r.id === reviewId ? updated : r)));
      this.cancelReply();
    } catch {
      this.toastService.notify('Не удалось отправить ответ', 'error');
    } finally {
      this.replyBusy.set(false);
    }
  }
}
