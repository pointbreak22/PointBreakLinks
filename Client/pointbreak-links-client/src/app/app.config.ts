import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import {
  ApplicationConfig,
  PLATFORM_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { jwtAuthInterceptor } from './core/http/jwt-auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { PaletteService } from './core/theme/palette.service';
import { NotificationsBootstrapService } from './core/signalr/notifications-bootstrap.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([jwtAuthInterceptor])),
    provideAppInitializer(() => {
      // Browser-only: the refresh token is an httpOnly cookie the SSR render on the server
      // has no access to (the incoming request isn't forwarded here), so attempting this
      // during SSR would just always fail. The client-side pass after hydration is what
      // actually restores the session; see UserStore for why nothing is persisted otherwise.
      if (!isPlatformBrowser(inject(PLATFORM_ID))) return;

      const authService = inject(AuthService);
      void authService.initializeSession();

      inject(PaletteService).init();

      // Instantiating this is enough — its constructor sets up an effect() that connects the
      // notification hub whenever a session exists (right now if initializeSession() above
      // already restored one, or the moment a later login/register creates one) and
      // disconnects it on logout, for the rest of the app's lifetime.
      inject(NotificationsBootstrapService);
    }),
  ],
};
