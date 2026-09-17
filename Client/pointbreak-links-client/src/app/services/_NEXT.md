# Feature API services

One thin `HttpClient` wrapper per API resource, same shape as `core/auth/auth.service.ts`
(inject `HttpClient`, hit `ApiEndpoints.*`, return typed promises). Add the matching entries to
`core/http/api-endpoints.ts` as each is built. No state here — state lives in the matching
`stores/*.store.ts` (see `stores/_NEXT.md`).

Done: `sites-api.service.ts` (seller-side + `getSitesByProject` + buyer-side `getCatalog()` with
real topic/country/price/iks/dr filters + `requestPublication()` + `getCountries()`/
`verifySite()` — same resource, one file), `stats-api.service.ts`,
`messages-api.service.ts` (paired with `shared/message-chat-modal/` — now also pushes over
SignalR, see `core/signalr/notifications-bootstrap.service.ts`, but this stays plain
request/response for the message list itself), `projects-api.service.ts`,
`admin-api.service.ts` (`getUsers`/`setBanned`/`setRole`/`getDashboard` — new module, not a port),
`moderation-api.service.ts` (`getPendingSites`/`approveSite`/`rejectSite` — new module, not a port),
`favorites-api.service.ts` (`getMyFavorites`/`getFavoriteSiteIds`/`addFavorite`/`removeFavorite` —
new module, not a port), `reviews-api.service.ts` (`getSiteReviews`/`createReview` — new module,
not a port), `notifications-api.service.ts` (`getMyNotifications`/`getUnreadCount`/`markAllRead` —
new module, paired with `stores/notifications.store.ts`), `analytics-api.service.ts`
(`getMyAnalytics()` — new module, paired with `pages/analytics/analytics.ts`; no store, same
fetch-once-on-init pattern as `purchased-site-timeline`). `sites-api.service.ts` also grew
`getPurchasedSiteEvents()` (paired with `shared/purchased-site-timeline/`) and
`exportWebmasterSales()`/`exportProjectSites()` (return `Blob`, downloaded via
`core/http/download-blob.ts` — same resource, not a new service).

`position`/`admin-api`/`system-settings` deliberately have no API service at all — they're
honest static mockups (see `pages/_NEXT.md`), not backed by any endpoint.

`wallet-api.service.ts` (`getBalance`/`getTransactions`/`topUp` — new module, paired with
`pages/wallet/` and `stores/user.store.ts`'s `balance` field/`setBalance()`, not its own store).

Still needed: nothing.
