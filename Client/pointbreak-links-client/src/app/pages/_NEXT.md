# Feature pages — not yet ported

Done: `landing` (FOXLinks' `pages/index.vue`), `auth/login`, `auth/register` (FOXLinks'
`pages/register.vue`), `coming-soon` (FOXLinks' `temp-unavailable.vue`), **`webmaster`**
(FOXLinks' `pages/webmaster.vue` + `page-components/webmaster/my-platforms.vue` +
`my-sales.vue` + their three modals — seller side only), **`projects`**
(`pages/projects/project-list/` + `project-details/` + `create-edit-project-modal/`, FOXLinks'
`pages/project/project-list.vue` + `project-details.vue` + the create/edit modal), and
**`optimizator`** (`pages/optimizator/optimizator.ts` + `buy-miralinks-modal/`, FOXLinks'
`pages/optimizator.vue` + `modal-windows/optimizator/buy-miralinks-modal.vue`),
**`admin/admin-dashboard` + `admin/admin-users`**, and **`moderation`** — three new modules, not
ports (see below). `projects` is now the post-login landing route for a regular user (matches
FOXLinks' own `router.push('/project/project-list')` after login/register) — the old placeholder
`/app` Dashboard was removed once this made it redundant. `UserStore.postLoginRoute` decides where
each role actually lands (`/admin-dashboard` for admin, `/projects` otherwise) — both `login.ts`
and `header.ts`'s `onLoginSuccess()` (the login-modal path) use it, so it's one place to extend
when a role gets its own real landing page.

**`moderation` is designed, not ported**: the `moderator` role existed in FOXLinks' seeded roles
table from the start but had no page or backend anywhere in the source — this was raised as one
of a few "what's missing for a real link marketplace with roles" ideas (wallet/balance, real-time
notifications via the already-scaffolded `NotificationHub`, site ratings, and this), and built
first since wallet was explicitly deferred. Along the way it surfaced a real, previously
unnoticed bug shared with FOXLinks itself: `CreateSiteCommandHandler` (and FOXLinks'
`SiteRepository::store()`) set a new listing's `fl_is_active`/`IsActive` to `true` immediately —
an unreviewed submission was already live and buyable in the Optimizator catalog the moment it
was created, and the seller's own "На модерации" badge never changed afterward since nothing
ever moved a Site past that status. Fixed here (not upstream, obviously): `IsActive` now starts
`false`, a new `"active"`/"Активна" `Status` row was added (`StatusNames.Active` — none of
FOXLinks' five seeded statuses fit "approved site"), and `pages/moderation/moderation.ts` gives
the `moderator` role (and admin) a real queue: `GET /api/moderation/sites`, approve (→ active,
`IsActive = true`) / reject (→ rejected). Gated by `roleGuard('moderator', 'admin')` — the first
use of the guard factory with more than one role.

**The rest of that "what's missing" list — favorites, real catalog filters, ratings, site
ownership verification — all landed too** (wallet stayed deferred): `pages/favorites/` (new
page), `optimizator.ts`'s filter panel and favorite-star column, `pages/projects/
leave-review-modal/` (opened from `project-details` once an order is published and unreviewed),
and `my-platforms.html`'s "Проверить"/verified-badge column. See `PROJECT_MAP.md`'s "Что уже
готово" for the full write-up, including the real bug this surfaced:
`order-task-modal.html` had no way to trigger `POST /sites/{id}/confirm-published` at all — the
endpoint existed from an earlier session but no button ever called it, so `IsPublished` could
never become `true`, which would have made the review feature permanently unreachable. Fixed by
adding a "Подтвердить публикацию" action once an order is accepted and not yet published.

**`admin/*` is designed, not ported**: FOXLinks' `admin-users.vue`/`admin-dashboard.vue` are
local mock data (`reactive([...])`, hardcoded stat cards + Chart.js on invented arrays) with zero
backing API — no `AdminController` or equivalent exists anywhere in the Laravel backend.
`admin-users`: `GET /api/admin/users` (paginated, real role/project-count data), role change,
ban/unban — with a real `User.IsBanned` column enforced at login and refresh
(`LoginCommandHandler`/`RefreshTokenCommandHandler`), not just a UI toggle. Deliberately dropped:
the fake balance column, the nonexistent "on moderation" user status, the bulk-actions
toolbar/checkboxes, filters, and the add-user modal (fabricating accounts isn't a real admin
action worth replicating). `admin-dashboard`: one real aggregation query (users/projects/active
sites/orders/revenue counts) instead of `GET /api/admin/users`'s sibling — no charts (no tracked
history to chart) and no "commission" card (`PurchasedSite` only stores the final price with the
10% commission already baked in, no separate base-price record to split it back out from — see
`AdminDashboardDto`'s comment). Both gated by `roleGuard('admin')` — its first real use.

**What was deliberately not ported from `optimizator.vue`**: the mass-selection toolbar
(checkboxes, "В избранное"/"В список"/"Купить выбранные"), the favorite-star column, both
filter panels ("Фильтрация" and "Расширенный поиск" — `applyFilters`/`applyAdvancedSearch` fake
a spinner via `setTimeout` with no network call, `resetFilters` just clears DOM inputs), and
the commented-out "Новый заказ" button — all either literally commented out or wired to
handlers with no real effect. `handleSearchInput` (plain client-side text filter over table
rows via raw DOM `textContent`, no advanced-search dependency) was real and is ported, just
reimplemented as a `computed()` signal filter instead of direct DOM manipulation.
`modal-windows/optimizator/create-order-modal.vue` and `components/optimizator/details-panel.vue`
were skipped entirely — the modal has no `v-model` on any field and its "Создать заказ" button
has no click handler at all, and its only trigger in `optimizator.vue` is commented out; the
details-panel component isn't imported/used anywhere (a `.details-panel` CSS class match was
the false-positive that first suggested otherwise). `buy-miralinks-modal.vue`'s "Итоги" tab
hardcoded fake values (link count always "2 шт.", project/insurance always "Не выбрано") and a
fake balance section with no backing data — ported with the summary computed for real from the
same reactive state the other two tabs use, and the balance section dropped since no wallet
backend exists (see "Известные ограничения" in `PROJECT_MAP.md`).

**Password reset/change, order history, CSV export, notifications inbox — new modules, not
ports**: answer to "what else can be added for this project". None of the four existed in
FOXLinks in working form (auth there has no forgot/reset-password flow at all; `Messages`/
`Notification` had no persistent inbox, just the modal-windows already noted as orphaned above).
`pages/auth/forgot-password/`, `pages/auth/reset-password/` (reads `?token=` from the query
string), and a new `pages/profile/` page (account info + change-password form) round out auth;
`header.html`'s profile dropdown gained a "Профиль" link. `pages/projects/project-details` and
`pages/webmaster/order-task-modal` both gained an order-history section via a new shared
`shared/purchased-site-timeline/` component (backed by `PurchasedSiteEvent` — see
`PROJECT_MAP.md`), toggled per-row in `project-details` the same way `my-platforms.html`'s
verification instructions expand (`expandedOrderId`/`instructionsSiteId` — same pattern, two
different pages). CSV export added a "Экспорт в CSV" button to both `my-sales.html` and
`project-details.html`, downloading through `core/http/download-blob.ts` since a plain `<a href>`
can't carry the Bearer token an authenticated `GET` needs. The header bell (previously the inert
placeholder noted in the table below) is now a real dropdown backed by `stores/notifications.store.ts`
— unread badge, list, "Прочитать все" — refreshed both on SignalR connect and after every live
push from `NotificationsBootstrapService`, so it reads the persisted row count from the server
rather than incrementing a local counter.

**`analytics`, `position`, `admin/admin-api`, `admin/system-settings` — the last four FOXLinks
pages, split into one real page and three honest mockups.** All four source files were read in
full before deciding, including a grep across FOXLinks' Laravel backend for anything each UI
element could plausibly be backed by. `analytics` came back real: monthly spend/earned lines,
an order-status breakdown, and a per-project table are genuinely computable from
`Project`/`PurchasedSite`/`Site` (new `Application/CQRS/Analytics` module), so that part was
built for real — CTR/visibility/SERP-position metrics were dropped since nothing in this app's
domain can compute them. `position`/`admin-api`/`system-settings` came back requiring entire
subsystems that don't exist anywhere in FOXLinks or here (a keyword/SERP-tracking domain plus a
third-party rank API for Position; an `ApiKey` model and a public REST surface to protect for
admin-api; a system-settings table for security/infra knobs that would normally live in server
config, not a DB row, for system-settings) — not porting gaps, features FOXLinks' own UI
describes but never built. Per explicit instruction to build all four rather than skip the
unbuildable three, those three are real Angular pages with the FOXLinks layout, but every
number is an honest "—" instead of copied hardcoded figures, every table has a real empty state,
every form control is `disabled`, and every action button surfaces a toast explaining why
nothing happens instead of a Vue `alert()`. `chart.js` is a real npm dependency now (first one in
this project), wrapped by the new `shared/chart-canvas/` — used only by `analytics`, and lazily
loaded with it, so it doesn't touch the initial bundle. `nav-items.ts`'s `implemented` flags for
Position/admin-api/system-settings flipped to `true`, and the sidebar's two hardcoded
`/coming-soon` "Аналитика" links now point at the real route.

**`pages/wallet/` — new module, not a port.** FOXLinks' `balance-topup-modal.vue` is orphaned
(never imported anywhere), and no `balance`/`wallet` table exists in any FOXLinks migration —
designed from scratch after being explicitly requested (see `PROJECT_MAP.md`'s write-up for the
full design: `User.Balance` + a `BalanceTransaction` ledger, real enforcement in
`RequestPublicationCommandHandler`/`AcceptOrderCommandHandler`, only the top-up itself is an
honest instant-credit stand-in for a real payment provider). `UserStore.currentUser` grew a
`balance` field, refreshed via `UserStore.setBalance()` right after any of the current user's
own actions that change it (top-up, buying as buyer, accepting as seller) — no SignalR push
needed since a balance change is always the current user's own doing. The header's static
"0.00 ₽" chip is now a real, clickable link to `/wallet`.

Shared building blocks, reuse rather than re-invent per page: `shared/layout/header`,
`shared/layout/footer`, `shared/layout/sidebar` (route-path-driven sections, webmaster tab
switching), `shared/login-modal`, `shared/toast`, `shared/pagination`, `shared/smart-grid`
(DynamicStat widgets), `shared/message-chat-modal` (real, API-backed chat — used by both
webmaster's my-sales and projects' project-details), `shared/directives/fade-in-on-scroll.ts`,
the palette system (`core/theme/palette.service.ts` + `src/styles/palettes/`), and the
app-shell CSS in `src/styles/design-system.css` (`.main-container`/`.content`/`.sidebar`/
`.workspace-header`/`.table-*`/`.search-*`/`.btn-small`/`.metric-value` etc.).

**On filtering styles / extracting components** (explicit user feedback, applies to every
remaining page): FOXLinks' own `<style scoped>` blocks repeat the same button/card/table/badge
rules per page rather than sharing them — don't copy that duplication forward. Check
`design-system.css` first; only page-specific one-off layout should be new CSS (prefer Tailwind
utilities inline for that instead). And when a page embeds something that's really a
self-contained widget (a modal, a dropdown, a chat panel), split it into its own component the
way `AddEditSiteModal`/`OrderTaskModal`/`CreateEditProjectModal` were, rather than keeping it
inline in the page.

Everything else maps from the FOXLinks Nuxt frontend (`frontend/app/`):

| FOXLinks (Nuxt)                                   | Role                                             |
| -------------------------------------------------- | ------------------------------------------------ |
| `pages/project-old.vue`                             | Double-checked in full (1918 lines) — genuinely nothing to adapt. Its project-list view uses the same `useProjectsStore()` as the current pages, but its stat cards are hardcoded and its bulk-actions/export buttons have no handlers; its "links" detail view is 100% placeholder — `linksData` is a hand-written object with fake entries for project id 1 only, empty arrays for the rest, and the actual table body is a literal empty `<div id="links-table-rows">` with a comment saying links "will load dynamically" (never implemented). `pages/project/project-list.vue` + `project-details.vue` (already ported, verified) fully and honestly replace everything real here. Skip |
| `pages/Test.vue`, `pages/knowledge.vue` | Dev debug page (prints API base to console) and a literally empty stub (just `<Header/>`, no content) respectively — not real pages, skip |
| `modal-windows/add-edit-user-modal.vue` | Not imported anywhere — `admin-users.vue` has its own inline modal instead. Orphaned, skip |
| `components/webmaster/filters-panel.vue`, `selection-actions.vue`, `details-panel.vue`, `components/paginator.vue` | None of these are imported anywhere in the FOXLinks source (confirmed via grep across the whole app) — orphaned leftovers from an earlier iteration, consistent with the dead mass-selection/filter code already found hand-rolled (not componentized) inside `webmaster.vue` itself. `components/pagination.vue` (different file, actually used) is the one already ported. Skip all four |
| `modal-windows/balance-topup-modal.vue`, `notifications-modal.vue` | Both confirmed orphaned — neither is imported anywhere in the FOXLinks source either (not just "no backend yet"). Neither header placeholder is inert anymore: the balance chip is real (see `pages/wallet/`, below) and the bell is a real dropdown (see the notifications-inbox paragraph below); the chat icon next to it remains a placeholder |

Build these as standalone, lazy-loaded routes (`loadComponent`) under `app.routes.ts`, with
`data: { palette: '...' }` (see `optimizator-palette.css` / `admin-palette.css` /
`analytics-palette.css` — all already ported). Once a nav target lands, flip its
`implemented: true` in `core/navigation/nav-items.ts` so Header actually links to it. Each page
needs a matching service in `services/` (see `services/_NEXT.md`) — follow the pattern in
`services/sites-api.service.ts` + `stores/sites.store.ts` (thin HTTP service, no state; store
holds signals and calls the service).

**Gotcha hit while building webmaster, worth knowing before touching routing/guards again:**
protected routes must stay `RenderMode.Client` in `app.routes.server.ts`. SSR cannot see the
httpOnly refresh cookie (different origin from the API), so an SSR-rendered guard always sees
"logged out" — with `RenderMode.Server` this produced a real 302 to `/login` that the client
then had to unwind, landing logged-in users on a generic page instead of the one they actually
requested. `RenderMode.Client` skips the guard server-side entirely and lets it run once,
correctly, after `AuthService.sessionReady` resolves client-side.
