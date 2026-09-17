import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { SidebarService } from '../../core/layout/sidebar.service';
import { Header } from '../../shared/layout/header/header';
import { Sidebar, WebmasterTab } from '../../shared/layout/sidebar/sidebar';
import { MyPlatforms } from './my-platforms/my-platforms';
import { MySales } from './my-sales/my-sales';

// Ported from FOXLinks' pages/webmaster.vue. The source page also carried a large amount of
// mass-selection/context-menu DOM-manipulation code that never actually wired into either tab
// component (dead code) — not ported; see my-platforms.ts/my-sales.ts for what's real.
@Component({
  selector: 'app-webmaster',
  imports: [Header, Sidebar, MyPlatforms, MySales],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './webmaster.html',
})
export class Webmaster {
  protected readonly sidebarService = inject(SidebarService);
  protected readonly activeTab = signal<WebmasterTab>('platforms');
}
