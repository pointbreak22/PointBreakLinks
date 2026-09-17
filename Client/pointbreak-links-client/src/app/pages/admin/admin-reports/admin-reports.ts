import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { AdminApiService } from '../../../services/admin-api.service';
import { ToastService } from '../../../core/notifications/toast.service';
import { downloadBlob } from '../../../core/http/download-blob';

type ReportKey = 'orders' | 'transactions' | 'users' | 'withdrawals';

// Backs the admin sidebar's "Отчёты" link (previously coming-soon). Three flat CSV exports over
// real data (AdminReportsController), same shape as the existing sales/project-sites exports —
// no invented numbers, no charts.
@Component({
  selector: 'app-admin-reports',
  imports: [Header, Sidebar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-reports.html',
})
export class AdminReports {
  private readonly adminApi = inject(AdminApiService);
  private readonly toastService = inject(ToastService);

  protected readonly exporting = signal<ReportKey | null>(null);

  async export(report: ReportKey): Promise<void> {
    this.exporting.set(report);
    try {
      const blob = await {
        orders: () => this.adminApi.exportOrders(),
        transactions: () => this.adminApi.exportTransactions(),
        users: () => this.adminApi.exportUsers(),
        withdrawals: () => this.adminApi.exportWithdrawals(),
      }[report]();
      downloadBlob(blob, `${report}.csv`);
    } catch {
      this.toastService.notify('Не удалось экспортировать данные', 'error');
    } finally {
      this.exporting.set(null);
    }
  }
}
