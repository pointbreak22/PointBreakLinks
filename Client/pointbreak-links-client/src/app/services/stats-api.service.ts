import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { DynamicStatDto } from '../core/models/stat.model';

@Injectable({ providedIn: 'root' })
export class StatsApiService {
  private readonly http = inject(HttpClient);

  getByPageKey(pageKey: string): Promise<DynamicStatDto[]> {
    return firstValueFrom(this.http.get<DynamicStatDto[]>(ApiEndpoints.stats.byPageKey(pageKey)));
  }
}
