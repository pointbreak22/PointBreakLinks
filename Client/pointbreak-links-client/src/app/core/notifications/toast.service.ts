import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastState {
  text: string;
  type: ToastType;
  visible: boolean;
}

const AUTO_DISMISS_MS = 3000;

// Single global toast, matching FOXLinks' useNotify()/Toast — one instance, rendered once in
// app.html, triggered from anywhere via inject(ToastService).notify(...).
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _state = signal<ToastState>({ text: '', type: 'info', visible: false });
  private dismissTimer: ReturnType<typeof setTimeout> | undefined;

  readonly state = this._state.asReadonly();

  notify(text: string, type: ToastType = 'info'): void {
    clearTimeout(this.dismissTimer);
    this._state.set({ text, type, visible: true });
    this.dismissTimer = setTimeout(() => {
      this._state.update((s) => ({ ...s, visible: false }));
    }, AUTO_DISMISS_MS);
  }
}
