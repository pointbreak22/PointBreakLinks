import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserStore } from '../../stores/user.store';
import { ToastService } from '../notifications/toast.service';

// Every roleGuard() call in app.routes.ts passes only 'admin' and/or 'moderator' — the two roles
// the backend's "RequireTwoFactor" policy (WebAPI/Authorization) gates. Kept as a plain array
// check rather than importing RoleNames from the API project (no such cross-project reference
// exists on the client) — these are the same seeded string values either way.
const PRIVILEGED_ROLES = ['admin', 'moderator'];

export const authGuard: CanActivateFn = async () => {
  // Every inject() call happens before the first await — inject() only works synchronously
  // within the active injection context, which is gone once execution resumes after an await.
  const authService = inject(AuthService);
  const userStore = inject(UserStore);
  const router = inject(Router);

  // Awaiting this closes the race on a hard reload of a protected route: without it, this
  // guard can run (and redirect to /login) before the cookie-based session restore in
  // AuthService.initializeSession() finishes — see sessionReady's own comment for the detail.
  await authService.sessionReady;

  return userStore.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

// Keeps an already-authenticated visitor off /login and /register.
export const guestGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const userStore = inject(UserStore);
  const router = inject(Router);

  await authService.sessionReady;

  return userStore.isAuthenticated() ? router.createUrlTree(['/projects']) : true;
};

// Factory rather than a single guard: role checks differ per route (e.g. admin vs moderator),
// so each route passes the role(s) it requires — see Domain/Constants/RoleNames.cs for values.
export const roleGuard = (...allowedRoles: string[]): CanActivateFn => {
  return async () => {
    const authService = inject(AuthService);
    const userStore = inject(UserStore);
    const router = inject(Router);
    const toastService = inject(ToastService);

    await authService.sessionReady;

    const user = userStore.currentUser();
    const roles = user?.roles ?? [];
    if (!roles.some((role) => allowedRoles.includes(role))) {
      return router.createUrlTree(['/projects']);
    }

    // Mirrors WebAPI's "RequireTwoFactor" policy on the Admin/Moderation/SupportStaff
    // controllers — every allowedRoles list passed to roleGuard() in this app is drawn from
    // {admin, moderator}, so this check applies whenever the role check above passed at all.
    // Catching it here (rather than only relying on the backend's 403) avoids a logged-in
    // admin/moderator without 2FA ever seeing the page shell before every API call inside it
    // starts failing.
    if (allowedRoles.some((role) => PRIVILEGED_ROLES.includes(role)) && !user?.twoFactorEnabled) {
      toastService.notify('Для доступа к этому разделу включите двухфакторную аутентификацию в профиле.', 'error');
      return router.createUrlTree(['/profile']);
    }

    return true;
  };
};
