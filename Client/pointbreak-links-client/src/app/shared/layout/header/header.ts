import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { ThemeService } from '../../../core/theme/theme.service';
import { adminNavItems, moderationNavItem, userNavItems } from '../../../core/navigation/nav-items';
import { NotificationsStore } from '../../../stores/notifications.store';
import { MessagesStore } from '../../../stores/messages.store';
import { UserStore } from '../../../stores/user.store';
import { LoginModal } from '../../login-modal/login-modal';

// Ported from FOXLinks' components/header.vue. Shown on every page (see app.html) — adapts
// its nav/actions to auth state and role, same as the source.
@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, NgTemplateOutlet, LoginModal, DatePipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './header.html',
})
export class Header {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  protected readonly userStore = inject(UserStore);
  protected readonly themeService = inject(ThemeService);
  protected readonly notificationsStore = inject(NotificationsStore);
  protected readonly messagesStore = inject(MessagesStore);

  // 'dark' was used by FOXLinks on a couple of pages (project/analytics) with a solid
  // background instead of the default translucent glass — not built yet, so only 'glass'
  // is exercised today, but the page-level knob stays.
  readonly variant = input<'glass' | 'dark'>('glass');

  private readonly currentUrl = signal(this.router.url);
  protected readonly isHome = computed(() => this.currentUrl() === '/');

  protected readonly showLoginModal = signal(false);
  protected readonly mobileMenuOpen = signal(false);
  protected readonly profileMenuOpen = signal(false);
  protected readonly notificationsMenuOpen = signal(false);

  protected readonly navItems = computed(() => {
    const base = this.userStore.isAdmin() ? adminNavItems : userNavItems;
    const canModerate = this.userStore.isAdmin() || this.userStore.hasRole('moderator');
    const items = canModerate ? [...base, moderationNavItem] : base;
    return items.filter((item) => item.implemented);
  });

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.currentUrl.set(this.router.url));
  }

  toggleProfileMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.profileMenuOpen.update((v) => !v);
  }

  closeProfileMenu(): void {
    this.profileMenuOpen.set(false);
  }

  toggleNotificationsMenu(event: MouseEvent): void {
    event.stopPropagation();
    const opening = !this.notificationsMenuOpen();
    this.notificationsMenuOpen.set(opening);
    if (opening) {
      void this.notificationsStore.fetchNotifications();
    }
  }

  closeNotificationsMenu(): void {
    this.notificationsMenuOpen.set(false);
  }

  async markAllNotificationsRead(): Promise<void> {
    await this.notificationsStore.markAllRead();
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }

  async onLoginSuccess(): Promise<void> {
    this.showLoginModal.set(false);
    await this.router.navigateByUrl(this.userStore.postLoginRoute());
  }

  async logout(): Promise<void> {
    this.closeProfileMenu();
    await this.authService.logout();
    await this.router.navigateByUrl('/');
  }
}
