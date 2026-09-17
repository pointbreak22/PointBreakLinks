import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { SidebarService } from '../../../core/layout/sidebar.service';
import { UserStore } from '../../../stores/user.store';

export type WebmasterTab = 'platforms' | 'sales';

// Ported from FOXLinks' components/sidebar.vue. Sections are route-path-driven, same as the
// source — most links point at /coming-soon (FOXLinks' temp-unavailable.vue) since those
// destinations don't exist yet either in the source or here.
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly router = inject(Router);
  protected readonly sidebarService = inject(SidebarService);
  protected readonly userStore = inject(UserStore);

  // Only meaningful on /webmaster — ignored (and hidden) everywhere else.
  readonly webmasterTab = input<WebmasterTab>('platforms');
  readonly webmasterTabChange = output<WebmasterTab>();

  private readonly currentUrl = signal(this.router.url);
  protected readonly isWebmaster = computed(() => this.currentUrl().startsWith('/webmaster'));
  protected readonly isProjectOrAnalytics = computed(
    () => this.currentUrl().startsWith('/project') || this.currentUrl().startsWith('/analytics'),
  );
  protected readonly isOptimizator = computed(() => this.currentUrl().startsWith('/optimizator'));
  protected readonly isPosition = computed(() => this.currentUrl().startsWith('/position'));
  protected readonly isModeration = computed(() => this.currentUrl().startsWith('/moderation'));

  protected readonly dropdowns = signal({ buy: false, project: false });

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        this.currentUrl.set(this.router.url);
        this.sidebarService.closeMobile();
      });
  }

  toggleDropdown(name: 'buy' | 'project'): void {
    this.dropdowns.update((current) => ({ ...current, [name]: !current[name] }));
  }

  setWebmasterTab(tab: WebmasterTab): void {
    this.webmasterTabChange.emit(tab);
  }
}
