import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PageMeta } from '../../core/models/pagination.model';

// Ported from FOXLinks' components/pagination.vue. Simple "show every page number" approach,
// same as the source — worth switching to an ellipsis-based scheme if `lastPage` ever gets large.
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pagination.html',
})
export class Pagination {
  readonly meta = input.required<PageMeta>();
  readonly pageChange = output<number>();

  protected readonly pages = computed(() => Array.from({ length: this.meta().lastPage }, (_, i) => i + 1));

  goTo(page: number): void {
    if (page < 1 || page > this.meta().lastPage) return;
    this.pageChange.emit(page);
  }
}
