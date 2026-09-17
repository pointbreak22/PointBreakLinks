import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { PagedResult } from '../core/models/pagination.model';
import { SiteDto } from '../core/models/site.model';

// Thin HTTP wrapper, no state — state lives in stores/favorites.store.ts. New module, not a
// port (FOXLinks' "Избранные площадки" links all pointed at /coming-soon).
@Injectable({ providedIn: 'root' })
export class FavoritesApiService {
  private readonly http = inject(HttpClient);

  getMyFavorites(page: number, perPage = 15): Promise<PagedResult<SiteDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<SiteDto>>(ApiEndpoints.favorites.mine, { params }));
  }

  getFavoriteSiteIds(): Promise<number[]> {
    return firstValueFrom(this.http.get<number[]>(ApiEndpoints.favorites.ids));
  }

  addFavorite(siteId: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.favorites.byId(siteId), {}));
  }

  removeFavorite(siteId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.favorites.byId(siteId)));
  }
}
