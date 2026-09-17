import { ChangeDetectionStrategy, Component, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { SidebarService } from '../../core/layout/sidebar.service';

// Ported from FOXLinks' pages/temp-unavailable.vue — the destination for every sidebar/nav
// link whose real page doesn't exist yet, on both sides of this port.
@Component({
  selector: 'app-coming-soon',
  imports: [Header, Sidebar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './coming-soon.html',
})
export class ComingSoon {
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  protected readonly sidebarService = inject(SidebarService);

  goHome(): void {
    void this.router.navigateByUrl('/');
  }

  refresh(): void {
    if (this.isBrowser) {
      window.location.reload();
    }
  }
}
