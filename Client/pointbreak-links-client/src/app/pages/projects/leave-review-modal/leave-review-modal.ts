import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { extractErrorMessage } from '../../../core/http/api-error';
import { PurchasedSiteDto } from '../../../core/models/site.model';
import { ToastService } from '../../../core/notifications/toast.service';
import { ReviewsApiService } from '../../../services/reviews-api.service';

// New component, not a port.
@Component({
  selector: 'app-leave-review-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leave-review-modal.html',
})
export class LeaveReviewModal {
  private readonly reviewsApi = inject(ReviewsApiService);
  private readonly toastService = inject(ToastService);

  readonly order = input.required<PurchasedSiteDto>();
  readonly close = output<void>();
  readonly submitted = output<void>();

  protected readonly rating = signal(5);
  protected readonly comment = signal('');
  protected readonly submitting = signal(false);

  protected readonly stars = [1, 2, 3, 4, 5];

  setRating(value: number): void {
    this.rating.set(value);
  }

  onCommentInput(event: Event): void {
    this.comment.set((event.target as HTMLTextAreaElement).value);
  }

  async submit(): Promise<void> {
    this.submitting.set(true);
    try {
      await this.reviewsApi.createReview(this.order().id, this.rating(), this.comment().trim() || null);
      this.toastService.notify('Отзыв добавлен', 'success');
      this.submitted.emit();
      this.close.emit();
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отправить отзыв'), 'error');
    } finally {
      this.submitting.set(false);
    }
  }
}
