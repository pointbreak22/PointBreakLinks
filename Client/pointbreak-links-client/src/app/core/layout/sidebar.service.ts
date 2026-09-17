import { Injectable, signal } from '@angular/core';

// Ported from FOXLinks' composables/useSidebar.js — a single collapsed/expanded flag shared
// between the Sidebar itself and any page that needs to know (e.g. to shift its content margin).
@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly _isCollapsed = signal(false);
  // Separate from isCollapsed: collapsed is the desktop narrow-rail state (still visible,
  // just icon-only); mobile-open is the ≤992px off-canvas drawer (hidden unless toggled open).
  private readonly _isMobileOpen = signal(false);

  readonly isCollapsed = this._isCollapsed.asReadonly();
  readonly isMobileOpen = this._isMobileOpen.asReadonly();

  toggle(): void {
    this._isCollapsed.update((v) => !v);
  }

  toggleMobile(): void {
    this._isMobileOpen.update((v) => !v);
  }

  closeMobile(): void {
    this._isMobileOpen.set(false);
  }
}
