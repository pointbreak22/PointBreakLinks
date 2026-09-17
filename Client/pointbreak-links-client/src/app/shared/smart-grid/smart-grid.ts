import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit } from '@angular/core';
import { StatsStore } from '../../stores/stats.store';

// Ported from FOXLinks' components/smart-grid.vue. No real-time push (source used Laravel
// Echo/websockets to live-update cards) — see StatsStore's comment for where that would land.
@Component({
  selector: 'app-smart-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './smart-grid.html',
})
export class SmartGrid implements OnInit {
  protected readonly statsStore = inject(StatsStore);

  readonly pageKey = input.required<string>();

  protected readonly currentStats = computed(() =>
    this.statsStore
      .stats()
      .filter((s) => s.pageKey === this.pageKey())
      .sort((a, b) => a.position - b.position),
  );

  ngOnInit(): void {
    void this.statsStore.fetchStats(this.pageKey());
  }
}
