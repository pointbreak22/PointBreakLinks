import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { SidebarService } from '../../../core/layout/sidebar.service';
import { ToastService } from '../../../core/notifications/toast.service';

// FOXLinks' system-settings.vue is a security-config form (2FA, password policy, CSRF/XSS
// toggles, rate limits, SSL cipher selection) with every toggle/value hardcoded in the
// template and no save handler wired to its "Сохранить настройки" button at all. There's no
// settings table anywhere in FOXLinks' backend, and most of these knobs (HSTS, cipher choice,
// CSRF protection) are normally server/framework config, not something a real app persists to
// a database and exposes as an editable admin-panel form (see PROJECT_MAP.md). Kept as a
// visual layout (per explicit request to build all four "coming soon" pages) but every
// control is disabled and unchecked/empty rather than pre-filled with FOXLinks' invented
// "current" values — nothing here claims to reflect real system state.
@Component({
  selector: 'app-system-settings',
  imports: [Header, Sidebar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './system-settings.html',
})
export class SystemSettings {
  protected readonly sidebarService = inject(SidebarService);
  private readonly toastService = inject(ToastService);

  notifyUnavailable(): void {
    this.toastService.notify('Системные настройки пока не реализованы — эти параметры нигде не сохраняются.', 'info');
  }
}
