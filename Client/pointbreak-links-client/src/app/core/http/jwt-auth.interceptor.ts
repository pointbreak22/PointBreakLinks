import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { ApiEndpoints } from './api-endpoints';
import { UserStore } from '../../stores/user.store';

// Attaches the in-memory access token to every request against our own API, and on a 401
// (access token expired — it's short-lived by design, see Jwt:AccessTokenMinutes) tries the
// httpOnly-cookie refresh flow exactly once before giving up. A handful of [AllowAnonymous]
// endpoints are excluded from that retry because a 401 from them never means "this bearer
// token expired" — refresh to avoid an infinite loop when the refresh call is itself the one
// that 401s (no valid session at all); login because a 401 there means "wrong credentials" or
// "banned account"; reset-password and confirm-email-change because a 401 there means "this
// one-time token is bad/expired" (AuthenticationException in ResetPasswordCommandHandler /
// ConfirmEmailChangeCommandHandler). Retrying any of these after a refresh attempt (which
// itself 401s when there's no session to refresh) replaced the real error message with the
// refresh call's unrelated one before it ever reached the page.
const NO_REFRESH_RETRY_PATHS: string[] = [
  ApiEndpoints.auth.refresh,
  ApiEndpoints.auth.login,
  ApiEndpoints.auth.resetPassword,
  ApiEndpoints.auth.confirmEmailChange,
];

export const jwtAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const userStore = inject(UserStore);
  const authService = inject(AuthService);

  const isApiRequest = req.url.startsWith(environment.apiBaseUrl);
  const skipRefreshRetry = NO_REFRESH_RETRY_PATHS.includes(req.url);

  const token = userStore.accessToken();
  const authorizedReq =
    isApiRequest && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` }, withCredentials: true })
    : isApiRequest ? req.clone({ withCredentials: true })
    : req;

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiRequest &&
        !skipRefreshRetry
      ) {
        return from(authService.refreshAccessToken()).pipe(
          switchMap((newToken) =>
            next(authorizedReq.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })),
          ),
          catchError((refreshError: unknown) => {
            userStore.clear();
            return throwError(() => refreshError);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
