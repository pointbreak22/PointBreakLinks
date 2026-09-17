import { Injectable, inject, signal } from '@angular/core';
import { StatsApiService } from '../services/stats-api.service';
import { DynamicStatDto } from '../core/models/stat.model';

// Ported from FOXLinks' stores/stats.ts. No real-time push here yet (source's version used
// Laravel Echo/websockets) — see WebAPI/Hubs/NotificationHub.cs for where that would land.
@Injectable({ providedIn: 'root' })
export class StatsStore {
  private readonly api = inject(StatsApiService);

  private readonly _stats = signal<DynamicStatDto[]>([]);
  private readonly _loading = signal(false);

  readonly stats = this._stats.asReadonly();
  readonly loading = this._loading.asReadonly();

  async fetchStats(pageKey: string): Promise<void> {
    this._loading.set(true);
    try {
      const incoming = await this.api.getByPageKey(pageKey);
      const others = this._stats().filter((s) => s.pageKey !== pageKey);
      this._stats.set([...others, ...incoming]);
    } finally {
      this._loading.set(false);
    }
  }
}
