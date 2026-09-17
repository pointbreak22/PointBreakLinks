import { Injectable, inject, signal } from '@angular/core';
import { SavedSearchesApiService } from '../services/saved-searches-api.service';
import { SavedSearchDto } from '../core/models/site.model';

@Injectable({ providedIn: 'root' })
export class SavedSearchesStore {
  private readonly api = inject(SavedSearchesApiService);

  private readonly _searches = signal<SavedSearchDto[]>([]);
  private readonly _loading = signal(false);

  readonly searches = this._searches.asReadonly();
  readonly loading = this._loading.asReadonly();

  async fetch(): Promise<void> {
    this._loading.set(true);
    try {
      this._searches.set(await this.api.getMine());
    } finally {
      this._loading.set(false);
    }
  }

  async create(payload: {
    topicId?: number | null;
    countryId?: number | null;
    minPrice?: number | null;
    maxPrice?: number | null;
    minIks?: number | null;
    minDr?: number | null;
  }): Promise<void> {
    const created = await this.api.create(payload);
    this._searches.update((searches) => [created, ...searches]);
  }

  async remove(id: number): Promise<void> {
    await this.api.remove(id);
    this._searches.update((searches) => searches.filter((s) => s.id !== id));
  }
}
