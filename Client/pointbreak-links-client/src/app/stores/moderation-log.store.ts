import { Injectable, inject, signal } from '@angular/core';
import { ModerationApiService } from '../services/moderation-api.service';
import { ModerationAuditEntryDto } from '../core/models/moderation.model';
import { PageMeta } from '../core/models/pagination.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

@Injectable({ providedIn: 'root' })
export class ModerationLogStore {
  private readonly api = inject(ModerationApiService);

  private readonly _entries = signal<ModerationAuditEntryDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _loading = signal(false);

  readonly entries = this._entries.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly loading = this._loading.asReadonly();

  async fetchAuditLog(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getAuditLog(page, this._meta().perPage);
      this._entries.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }
}
