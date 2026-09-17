import { Injectable, computed, signal } from '@angular/core';

export interface CurrentUser {
  id: number;
  name: string;
  email: string;
  roles: string[];
  twoFactorEnabled: boolean;
  balance: number;
}

// Access token lives only in memory (never localStorage/sessionStorage): it's SSR-safe and,
// since the refresh token is an httpOnly cookie the JS layer never touches, there's nothing
// worth persisting client-side anyway — a hard refresh just re-runs the silent-refresh flow
// in app.config.ts.
@Injectable({ providedIn: 'root' })
export class UserStore {
  private readonly _currentUser = signal<CurrentUser | null>(null);
  private readonly _accessToken = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.roles.includes('admin') ?? false);

  // Where login/register should land the user — used by login.ts, login-modal (via header.ts)
  // and register.ts, so it only needs updating in one place as more role-specific landing
  // pages appear.
  readonly postLoginRoute = computed(() => (this.isAdmin() ? '/admin-dashboard' : '/projects'));

  hasRole(role: string): boolean {
    return this._currentUser()?.roles.includes(role) ?? false;
  }

  setSession(user: CurrentUser, accessToken: string): void {
    this._currentUser.set(user);
    this._accessToken.set(accessToken);
  }

  setAccessToken(accessToken: string): void {
    this._accessToken.set(accessToken);
  }

  // Called after any action that changes the current user's own balance (top-up, placing an
  // order as buyer, accepting an order as seller) — cheaper than re-fetching the whole session,
  // and always driven by that user's own action, so there's nothing to keep live via SignalR.
  setBalance(balance: number): void {
    const user = this._currentUser();
    if (user) {
      this._currentUser.set({ ...user, balance });
    }
  }

  // Called after enabling/disabling 2FA (profile page) — cheaper than a full session refetch.
  setTwoFactorEnabled(enabled: boolean): void {
    const user = this._currentUser();
    if (user) {
      this._currentUser.set({ ...user, twoFactorEnabled: enabled });
    }
  }

  clear(): void {
    this._currentUser.set(null);
    this._accessToken.set(null);
  }
}
