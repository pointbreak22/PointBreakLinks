import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { SidebarService } from '../../../core/layout/sidebar.service';
import { ToastService } from '../../../core/notifications/toast.service';

// FOXLinks' admin-api.vue describes a developer-facing API-key management feature (create
// keys, per-key rate limits, request counters) that was never actually built anywhere in
// FOXLinks — no ApiKey model/migration, no api-key route, and the app has no public REST
// surface for third parties to consume with a key in the first place (see PROJECT_MAP.md).
// This isn't a porting gap, it's speculative UI for a product feature that doesn't exist. Kept
// as a visual layout (per explicit request to build all four "coming soon" pages) but with
// honest "—" placeholders instead of FOXLinks' invented key list/stats, and no fabricated
// "https://api.foxlinks.ru/v1" base URL — this app has no public API to document.
@Component({
  selector: 'app-admin-api',
  imports: [Header, Sidebar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-api.html',
})
export class AdminApi {
  protected readonly sidebarService = inject(SidebarService);
  private readonly toastService = inject(ToastService);

  notifyUnavailable(): void {
    this.toastService.notify('Управление API-ключами пока не реализовано — публичного API для внешних интеграций нет.', 'info');
  }
}
