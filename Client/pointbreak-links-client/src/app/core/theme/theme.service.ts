import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'theme';

// Light/dark toggle, independent of PaletteService (which picks *which* palette's tokens
// apply). Persisted to localStorage — a per-viewer display preference, not session state,
// so unlike UserStore this is exactly what localStorage is for.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = signal<Theme>(this.readInitialTheme());

  constructor() {
    this.applyClass(this.theme());
  }

  toggle(): void {
    this.set(this.theme() === 'dark' ? 'light' : 'dark');
  }

  set(theme: Theme): void {
    this.theme.set(theme);
    this.applyClass(theme);
    if (this.isBrowser) {
      try {
        localStorage.setItem(STORAGE_KEY, theme);
      } catch {
        // Private browsing / storage disabled — theme just won't persist across reloads.
      }
    }
  }

  private readInitialTheme(): Theme {
    if (!this.isBrowser) return 'light';
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved === 'dark' ? 'dark' : 'light';
    } catch {
      return 'light';
    }
  }

  private applyClass(theme: Theme): void {
    if (!this.isBrowser) return;
    document.body.classList.toggle('dark-theme', theme === 'dark');
    document.body.classList.toggle('light-theme', theme === 'light');
  }
}
