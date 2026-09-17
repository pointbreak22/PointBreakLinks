import { Injectable, inject, signal } from '@angular/core';
import { ModerationApiService } from '../services/moderation-api.service';
import { PendingSiteDto } from '../core/models/moderation.model';
import { PageMeta } from '../core/models/pagination.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

@Injectable({ providedIn: 'root' })
export class ModerationStore {
  private readonly api = inject(ModerationApiService);

  private readonly _sites = signal<PendingSiteDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _loading = signal(false);
  private readonly _selectedIds = signal<ReadonlySet<number>>(new Set());
  private readonly _bulkBusy = signal(false);

  readonly sites = this._sites.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly selectedIds = this._selectedIds.asReadonly();
  readonly bulkBusy = this._bulkBusy.asReadonly();

  async fetchPendingSites(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getPendingSites(page, this._meta().perPage);
      this._sites.set(response.items);
      this._meta.set(response);
      this._selectedIds.set(new Set());
    } finally {
      this._loading.set(false);
    }
  }

  async approveSite(id: number): Promise<void> {
    await this.api.approveSite(id);
    this.dropFromList([id]);
  }

  async rejectSite(id: number, reason?: string): Promise<void> {
    await this.api.rejectSite(id, reason);
    this.dropFromList([id]);
  }

  toggleSelected(id: number): void {
    const next = new Set(this._selectedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this._selectedIds.set(next);
  }

  toggleSelectAll(): void {
    this._selectedIds.set(this._selectedIds().size === this._sites().length ? new Set() : new Set(this._sites().map((s) => s.id)));
  }

  async bulkApproveSelected(): Promise<void> {
    const ids = [...this._selectedIds()];
    this._bulkBusy.set(true);
    try {
      const result = await this.api.bulkApprove(ids);
      this.dropFromList(ids.filter((id) => !result.failed.includes(id)));
    } finally {
      this._bulkBusy.set(false);
    }
  }

  async bulkRejectSelected(reason?: string): Promise<void> {
    const ids = [...this._selectedIds()];
    this._bulkBusy.set(true);
    try {
      const result = await this.api.bulkReject(ids, reason);
      this.dropFromList(ids.filter((id) => !result.failed.includes(id)));
    } finally {
      this._bulkBusy.set(false);
    }
  }

  private dropFromList(ids: number[]): void {
    const idSet = new Set(ids);
    this._sites.update((sites) => sites.filter((s) => !idSet.has(s.id)));
    const nextSelected = new Set(this._selectedIds());
    for (const id of ids) nextSelected.delete(id);
    this._selectedIds.set(nextSelected);
  }
}
