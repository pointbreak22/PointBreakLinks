import { RenderMode, ServerRoute } from '@angular/ssr';

// Public routes (landing, login, register) are still Server-rendered — nothing there depends
// on knowing the real session server-side, so per-request SSR is a straight win (fast first
// paint, indexable).
//
// Everything auth-gated (app/webmaster/coming-soon/...) is Client instead: SSR genuinely
// cannot know whether the visitor is logged in — the httpOnly refresh cookie lives on the API's
// origin, not this app's, so it never reaches the SSR request at all — and guessing "logged
// out" was actively wrong. It used to render the guard's redirect to /login server-side, the
// browser followed that as a real 302, and by the time the client rehydrated and confirmed the
// session was in fact valid, the route had already moved past /login to a generic /app instead
// of the page the user actually asked for (e.g. requesting /webmaster while logged in landed on
// /app, not /webmaster). Skipping SSR for these routes means the guard only ever runs once,
// client-side, once AuthService.sessionReady has resolved — see auth.guard.ts.
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server },
  { path: 'login', renderMode: RenderMode.Server },
  { path: 'register', renderMode: RenderMode.Server },
  { path: '**', renderMode: RenderMode.Client },
];
