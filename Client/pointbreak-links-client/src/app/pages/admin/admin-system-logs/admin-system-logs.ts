import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { AdminStore } from '../../../stores/admin.store';

// Backs the admin sidebar's "Логи системы" link (previously a dead <li>, no link at all).
// "System logs" here means the real PurchasedSiteEvent timeline (application created,
// accepted, published, reviewed) across every order platform-wide — there's no separate
// application/error-log store anywhere in this app, and inventing one wasn't the ask.
@Component({
  selector: 'app-admin-system-logs',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-system-logs.html',
})
export class AdminSystemLogs implements OnInit {
  protected readonly adminStore = inject(AdminStore);

  ngOnInit(): void {
    void this.adminStore.fetchSystemLogs();
  }

  changePage(page: number): void {
    void this.adminStore.fetchSystemLogs(page);
  }
}
