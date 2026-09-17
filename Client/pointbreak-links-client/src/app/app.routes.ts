import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guard';
import { Landing } from './pages/landing/landing';

// '' is the public marketing landing page (FOXLinks' pages/index.vue), not the app itself —
// it's eagerly imported since it IS the entry point (lazy-loading it would just add a
// round trip before anything renders). It's shown to everyone, logged in or not; Header
// adapts to auth state on its own rather than the route redirecting away.
export const routes: Routes = [
  { path: '', component: Landing, data: { palette: 'index' } },
  {
    // Public, no authGuard — reachable from the anonymous marketing header/footer as well as
    // the authenticated sidebar.
    path: 'knowledge',
    data: { palette: 'index' },
    loadComponent: () => import('./pages/knowledge/knowledge').then((m) => m.Knowledge),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    data: { palette: 'register' },
    loadComponent: () => import('./pages/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    data: { palette: 'register' },
    loadComponent: () => import('./pages/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    data: { palette: 'register' },
    loadComponent: () => import('./pages/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    data: { palette: 'register' },
    loadComponent: () => import('./pages/auth/reset-password/reset-password').then((m) => m.ResetPassword),
  },
  {
    path: 'confirm-email-change',
    data: { palette: 'register' },
    loadComponent: () => import('./pages/auth/confirm-email-change/confirm-email-change').then((m) => m.ConfirmEmailChange),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile),
  },
  {
    path: 'wallet',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/wallet/wallet').then((m) => m.Wallet),
  },
  {
    path: 'messages',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/messages/messages').then((m) => m.Messages),
  },
  {
    path: 'support',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/support/support').then((m) => m.Support),
  },
  // The post-login landing spot for a regular user — matches FOXLinks (router.push to
  // /project/project-list after login/register). There used to be a placeholder /app
  // Dashboard here before Projects was a real page; removed once this made it redundant.
  {
    path: 'projects',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/projects/project-list/project-list').then((m) => m.ProjectList),
  },
  {
    path: 'projects/:id',
    canActivate: [authGuard],
    data: { palette: 'project' },
    loadComponent: () => import('./pages/projects/project-details/project-details').then((m) => m.ProjectDetails),
  },
  {
    path: 'webmaster',
    canActivate: [authGuard],
    data: { palette: 'webmaster' },
    loadComponent: () => import('./pages/webmaster/webmaster').then((m) => m.Webmaster),
  },
  {
    path: 'optimizator',
    canActivate: [authGuard],
    data: { palette: 'optimizator' },
    loadComponent: () => import('./pages/optimizator/optimizator').then((m) => m.Optimizator),
  },
  {
    path: 'analytics',
    canActivate: [authGuard],
    data: { palette: 'analytics' },
    loadComponent: () => import('./pages/analytics/analytics').then((m) => m.Analytics),
  },
  {
    path: 'position',
    canActivate: [authGuard],
    data: { palette: 'position' },
    loadComponent: () => import('./pages/position/position').then((m) => m.Position),
  },
  {
    path: 'favorites',
    canActivate: [authGuard],
    data: { palette: 'optimizator' },
    loadComponent: () => import('./pages/favorites/favorites').then((m) => m.Favorites),
  },
  {
    path: 'sellers/:id',
    canActivate: [authGuard],
    data: { palette: 'optimizator' },
    loadComponent: () => import('./pages/seller-profile/seller-profile').then((m) => m.SellerProfile),
  },
  {
    path: 'moderation',
    canActivate: [authGuard, roleGuard('moderator', 'admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/moderation/moderation').then((m) => m.Moderation),
  },
  {
    path: 'moderation/log',
    canActivate: [authGuard, roleGuard('moderator', 'admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/moderation-log/moderation-log').then((m) => m.ModerationLog),
  },
  {
    path: 'admin-dashboard',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-dashboard/admin-dashboard').then((m) => m.AdminDashboard),
  },
  {
    path: 'admin-users',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-users/admin-users').then((m) => m.AdminUsers),
  },
  {
    path: 'admin-api',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-api/admin-api').then((m) => m.AdminApi),
  },
  {
    path: 'system-settings',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/system-settings/system-settings').then((m) => m.SystemSettings),
  },
  {
    path: 'admin-reports',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-reports/admin-reports').then((m) => m.AdminReports),
  },
  {
    path: 'admin-projects',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-projects/admin-projects').then((m) => m.AdminProjects),
  },
  {
    path: 'admin-transactions',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-transactions/admin-transactions').then((m) => m.AdminTransactions),
  },
  {
    path: 'admin-system-logs',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-system-logs/admin-system-logs').then((m) => m.AdminSystemLogs),
  },
  {
    path: 'admin-support',
    canActivate: [authGuard, roleGuard('admin', 'moderator')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-support/admin-support').then((m) => m.AdminSupport),
  },
  {
    path: 'admin-disputes',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-disputes/admin-disputes').then((m) => m.AdminDisputes),
  },
  {
    path: 'admin-withdrawals',
    canActivate: [authGuard, roleGuard('admin')],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/admin/admin-withdrawals/admin-withdrawals').then((m) => m.AdminWithdrawals),
  },
  {
    path: 'coming-soon',
    canActivate: [authGuard],
    data: { palette: 'admin' },
    loadComponent: () => import('./pages/coming-soon/coming-soon').then((m) => m.ComingSoon),
  },
  { path: '**', redirectTo: '' },
];
