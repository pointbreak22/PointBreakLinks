import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../http/api-endpoints';
import { LoginHistoryEntryDto } from '../models/two-factor.model';
import { CurrentUser, UserStore } from '../../stores/user.store';

interface AuthResponse {
  access_token: string;
  user: CurrentUser;
}

interface TwoFactorRequiredResponse {
  requiresTwoFactor: true;
  ticket: string;
}

export type LoginOutcome = { requiresTwoFactor: false } | { requiresTwoFactor: true; ticket: string };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly userStore = inject(UserStore);

  // Resolves once the initial silent-refresh from initializeSession() has settled (success or
  // failure) — route guards await this before checking isAuthenticated(), otherwise a hard
  // reload of a protected route races the refresh call: the guard runs before the cookie-based
  // session restore finishes, sees isAuthenticated() still false, and bounces to /login even
  // though the user is validly logged in (the header then flips to "logged in" a moment later,
  // but the router has already committed the wrong redirect and won't undo it).
  // On the server there is nothing to wait for — initializeSession() never runs there (see
  // app.config.ts) since the httpOnly cookie isn't forwarded into the SSR render — so this
  // resolves immediately to avoid guards hanging during SSR.
  private resolveSessionReady!: () => void;
  readonly sessionReady = new Promise<void>((resolve) => {
    this.resolveSessionReady = resolve;
  });

  constructor() {
    if (!isPlatformBrowser(inject(PLATFORM_ID))) {
      this.resolveSessionReady();
    }
  }

  async register(name: string, email: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(ApiEndpoints.auth.register, { name, email, password }, { withCredentials: true }),
    );
    this.userStore.setSession(response.user, response.access_token);
  }

  async login(email: string, password: string): Promise<LoginOutcome> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse | TwoFactorRequiredResponse>(
        ApiEndpoints.auth.login,
        { email, password },
        { withCredentials: true },
      ),
    );

    if ('requiresTwoFactor' in response) {
      return { requiresTwoFactor: true, ticket: response.ticket };
    }

    this.userStore.setSession(response.user, response.access_token);
    return { requiresTwoFactor: false };
  }

  // Second step of login when the account has 2FA enabled — ticket comes from login()'s
  // requiresTwoFactor branch, code from the user's authenticator app. rememberDevice, when true,
  // makes the server also set a long-lived "trusted_device" cookie that skips this whole step on
  // a later login from the same browser (LoginCommandHandler checks it before minting a ticket).
  async completeTwoFactorLogin(ticket: string, code: string, rememberDevice = false): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(ApiEndpoints.auth.loginTwoFactor, { ticket, code, rememberDevice }, { withCredentials: true }),
    );
    this.userStore.setSession(response.user, response.access_token);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.auth.logout, {}, { withCredentials: true }));
    } finally {
      this.userStore.clear();
    }
  }

  async forgotPassword(email: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.forgotPassword, { email }));
  }

  async resetPassword(token: string, newPassword: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.resetPassword, { token, newPassword }));
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.changePassword, { currentPassword, newPassword }));
  }

  async changeEmail(newEmail: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.changeEmail, { newEmail, password }));
  }

  async confirmEmailChange(token: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.confirmEmailChange, { token }));
  }

  async deactivateAccount(password: string): Promise<void> {
    await firstValueFrom(this.http.post(ApiEndpoints.auth.deactivateAccount, { password }, { withCredentials: true }));
    // Only reached on success (a wrong password rejects above) — server-side refresh
    // token/cookies are already gone (see AuthController), this just clears the in-memory
    // session so the UI reflects being logged out immediately.
    this.userStore.clear();
  }

  getLoginHistory(): Promise<LoginHistoryEntryDto[]> {
    return firstValueFrom(this.http.get<LoginHistoryEntryDto[]>(ApiEndpoints.auth.loginHistory));
  }

  // Called once from app.config.ts's app initializer. The refresh token travels as an
  // httpOnly cookie, so a valid session survives a hard reload without any token ever
  // sitting in localStorage — this just asks the API "is there a live session?".
  // Deliberately swallows failures: an anonymous visitor hitting this on every load is
  // the expected case, not an error.
  async initializeSession(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.post<AuthResponse>(ApiEndpoints.auth.refresh, {}, { withCredentials: true }),
      );
      this.userStore.setSession(response.user, response.access_token);
    } catch {
      this.userStore.clear();
    } finally {
      this.resolveSessionReady();
    }
  }

  // Used by the 401-retry path in jwt-auth.interceptor.ts — same request as
  // initializeSession, but callers there want the token back rather than a fire-and-forget.
  async refreshAccessToken(): Promise<string> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(ApiEndpoints.auth.refresh, {}, { withCredentials: true }),
    );
    this.userStore.setSession(response.user, response.access_token);
    return response.access_token;
  }
}
