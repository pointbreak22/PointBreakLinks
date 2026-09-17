import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { AnalyticsDto } from '../core/models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);

  getMyAnalytics(): Promise<AnalyticsDto> {
    return firstValueFrom(this.http.get<AnalyticsDto>(ApiEndpoints.analytics.mine));
  }
}
