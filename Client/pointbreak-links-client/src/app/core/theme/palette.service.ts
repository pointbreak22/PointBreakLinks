import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

// Declarative equivalent of FOXLinks' per-page `useHead({ bodyAttrs: { 'data-palette': ... } })`
// calls — one place, driven by `data: { palette: '...' }` on each route (see app.routes.ts),
// instead of every page component setting the body attribute itself.
@Injectable({ providedIn: 'root' })
export class PaletteService {
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  init(): void {
    if (!this.isBrowser) return;

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.applyFromRoute();
    });
    this.applyFromRoute();
  }

  private applyFromRoute(): void {
    let route: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;
    let palette: string | undefined;
    while (route) {
      palette = (route.data['palette'] as string | undefined) ?? palette;
      route = route.firstChild;
    }

    if (palette) {
      document.body.setAttribute('data-palette', palette);
    } else {
      document.body.removeAttribute('data-palette');
    }
  }
}
