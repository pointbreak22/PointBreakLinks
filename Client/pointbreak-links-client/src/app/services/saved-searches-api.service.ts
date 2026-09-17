import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { SavedSearchDto } from '../core/models/site.model';

// Thin HTTP wrapper, no state — state lives in stores/saved-searches.store.ts. New module, not
// a port (optimizator.vue's filter panels were entirely decorative — see SiteCatalogFilter's
// comment).
@Injectable({ providedIn: 'root' })
export class SavedSearchesApiService {
  private readonly http = inject(HttpClient);

  getMine(): Promise<SavedSearchDto[]> {
    return firstValueFrom(this.http.get<SavedSearchDto[]>(ApiEndpoints.savedSearches.mine));
  }

  create(payload: {
    topicId?: number | null;
    countryId?: number | null;
    minPrice?: number | null;
    maxPrice?: number | null;
    minIks?: number | null;
    minDr?: number | null;
  }): Promise<SavedSearchDto> {
    return firstValueFrom(this.http.post<SavedSearchDto>(ApiEndpoints.savedSearches.mine, payload));
  }

  remove(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.savedSearches.byId(id)));
  }
}
