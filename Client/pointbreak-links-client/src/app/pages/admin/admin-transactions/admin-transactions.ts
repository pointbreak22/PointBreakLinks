import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { AdminStore } from '../../../stores/admin.store';

// Backs the admin sidebar's "Транзакции" link (previously a dead <li>, no link at all). Every
// wallet transaction on the platform — same data as the "Транзакции" CSV export
// (AdminReportsController), just paginated for on-screen browsing instead of a bulk download.
@Component({
  selector: 'app-admin-transactions',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-transactions.html',
})
export class AdminTransactions implements OnInit {
  protected readonly adminStore = inject(AdminStore);

  ngOnInit(): void {
    void this.adminStore.fetchTransactions();
  }

  changePage(page: number): void {
    void this.adminStore.fetchTransactions(page);
  }

  typeLabel(type: string): string {
    return { TopUp: 'Пополнение', Purchase: 'Покупка', Sale: 'Продажа' }[type] ?? type;
  }
}
