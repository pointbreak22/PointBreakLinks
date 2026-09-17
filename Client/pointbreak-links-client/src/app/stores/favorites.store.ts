import { Injectable, inject, signal } from '@angular/core';
import { FavoritesApiService } from '../services/favorites-api.service';
import { SiteDto } from '../core/models/site.model';
import { PageMeta } from '../core/models/pagination.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

@Injectable({ providedIn: 'root' })
export class FavoritesStore {
  private readonly api = inject(FavoritesApiService);

  private readonly _sites = signal<SiteDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _loading = signal(false);
  // Overlay for the catalog table's star toggle — which site ids are favorited, independent of
  // whichever page/list is currently being viewed.
  private readonly _favoriteIds = signal<Set<number>>(new Set());

  readonly sites = this._sites.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly favoriteIds = this._favoriteIds.asReadonly();

  async fetchFavoriteIds(): Promise<void> {
    const ids = await this.api.getFavoriteSiteIds();
    this._favoriteIds.set(new Set(ids));
  }

  async fetchFavorites(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getMyFavorites(page, this._meta().perPage);
      this._sites.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  async toggleFavorite(siteId: number): Promise<void> {
    const isFavorited = this._favoriteIds().has(siteId);
    if (isFavorited) {
      await this.api.removeFavorite(siteId);
      this._favoriteIds.update((ids) => {
        const next = new Set(ids);
        next.delete(siteId);
        return next;
      });
      this._sites.update((sites) => sites.filter((s) => s.id !== siteId));
    } else {
      await this.api.addFavorite(siteId);
      this._favoriteIds.update((ids) => new Set(ids).add(siteId));
    }
  }
}
