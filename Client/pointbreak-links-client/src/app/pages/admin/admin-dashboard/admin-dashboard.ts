import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { AdminStore } from '../../../stores/admin.store';

// New page, not a port — FOXLinks' admin-dashboard.vue hardcodes all 4 stat cards (e.g.
// "Всего пользователей: 1,247") and both of its Chart.js graphs render invented arrays, with no
// backing API at all. Built for real instead, with only what a single aggregation query can
// honestly answer (see Application/CQRS/Admin/DTOs/AdminDashboardDto.cs for what was dropped
// and why) — no charts, since there's no tracked history to chart yet, and no fake trend
// arrows like the SmartGrid widgets on other pages show (those come from a real week-over-week
// computation; this dashboard doesn't have an equivalent time series behind it).
@Component({
  selector: 'app-admin-dashboard',
  imports: [Header, Sidebar, DecimalPipe, DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard implements OnInit {
  protected readonly adminStore = inject(AdminStore);

  ngOnInit(): void {
    void this.adminStore.fetchDashboard();
  }
}
