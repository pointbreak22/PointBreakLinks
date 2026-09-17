export interface NavItem {
  label: string;
  icon: string;
  path: string;
  // Flip to true once the target route/page actually exists — see the pages/_NEXT.md
  // breadcrumb. Header filters these out so it never links to a route that 404s.
  implemented: boolean;
}

// Mirrors header.vue's two nav branches (regular user vs admin). Kept as plain data so
// adding a page later is a one-line flip of `implemented`, not a header.ts change.
export const userNavItems: NavItem[] = [
  { label: 'Панель управления', icon: 'fa-th-large', path: '/projects', implemented: true },
  { label: 'Покупка ссылок', icon: 'fa-shopping-cart', path: '/optimizator', implemented: true },
  { label: 'Продажа ссылок', icon: 'fa-store', path: '/webmaster', implemented: true },
  { label: 'Мониторинг позиций', icon: 'fa-chart-line', path: '/position', implemented: true },
];

export const adminNavItems: NavItem[] = [
  { label: 'Панель управления', icon: 'fa-th-large', path: '/admin-dashboard', implemented: true },
  { label: 'Пользователи', icon: 'fa-users', path: '/admin-users', implemented: true },
  { label: 'Управление API', icon: 'fa-code', path: '/admin-api', implemented: true },
  { label: 'Настройка системы', icon: 'fa-cogs', path: '/system-settings', implemented: true },
];

// Not from FOXLinks — the `moderator` role existed in its seed data but had no page anywhere in
// the source. Shown to admins too (Header appends it whenever isAdmin() || hasRole('moderator'))
// since ModerationController authorizes both.
export const moderationNavItem: NavItem = {
  label: 'Модерация',
  icon: 'fa-clipboard-check',
  path: '/moderation',
  implemented: true,
};
