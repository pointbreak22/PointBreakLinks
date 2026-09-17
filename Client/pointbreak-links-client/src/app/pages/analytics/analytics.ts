import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import type { ChartConfiguration } from 'chart.js';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { ChartCanvas } from '../../shared/chart-canvas/chart-canvas';
import { SidebarService } from '../../core/layout/sidebar.service';
import { AnalyticsDto } from '../../core/models/analytics.model';
import { AnalyticsApiService } from '../../services/analytics-api.service';

// New page, not a port — FOXLinks' analytics.vue renders 6 Chart.js widgets on hardcoded
// arrays (spend numbers, a fake project table, invented "visibility %"/CTR figures) with zero
// backing API (see PROJECT_MAP.md). Built for real instead, with only what a genuine
// aggregation over this user's own Project/PurchasedSite/Site rows can honestly answer —
// spend/earnings over time, order-status breakdown, per-project totals. No SERP-derived
// metrics (visibility, CTR, average position): that data doesn't exist anywhere in this app's
// domain, see pages/position/position.ts for the page that would need it.
@Component({
  selector: 'app-analytics',
  imports: [Header, Sidebar, DecimalPipe, ChartCanvas],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './analytics.html',
})
export class Analytics implements OnInit {
  private readonly api = inject(AnalyticsApiService);
  protected readonly sidebarService = inject(SidebarService);

  protected readonly data = signal<AnalyticsDto | null>(null);
  protected readonly loading = signal(false);

  protected readonly spendVsEarnedConfig = computed<ChartConfiguration | null>(() => {
    const d = this.data();
    if (!d) return null;
    return {
      type: 'line',
      data: {
        labels: d.spendByMonth.map((p) => p.month),
        datasets: [
          {
            label: 'Потрачено (покупка)',
            data: d.spendByMonth.map((p) => p.value),
            borderColor: '#ff9b00',
            backgroundColor: 'rgba(255,155,0,0.15)',
            tension: 0.3,
            fill: true,
          },
          {
            label: 'Заработано (продажа)',
            data: d.earnedByMonth.map((p) => p.value),
            borderColor: '#28b446',
            backgroundColor: 'rgba(40,180,70,0.15)',
            tension: 0.3,
            fill: true,
          },
        ],
      },
      options: { responsive: true, maintainAspectRatio: false },
    };
  });

  protected readonly statusBreakdownConfig = computed<ChartConfiguration | null>(() => {
    const d = this.data();
    if (!d || d.ordersByStatus.length === 0) return null;
    return {
      type: 'doughnut',
      data: {
        labels: d.ordersByStatus.map((s) => s.description),
        datasets: [
          {
            data: d.ordersByStatus.map((s) => s.count),
            backgroundColor: ['#ff9b00', '#28b446', '#3b82f6', '#e74c3c', '#a855f7'],
          },
        ],
      },
      options: { responsive: true, maintainAspectRatio: false },
    };
  });

  ngOnInit(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.data.set(await this.api.getMyAnalytics());
    } finally {
      this.loading.set(false);
    }
  }
}
