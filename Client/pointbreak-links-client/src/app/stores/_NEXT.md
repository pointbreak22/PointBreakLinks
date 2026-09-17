# Feature stores

Signal-based, same shape (private writable signals + `.asReadonly()` + `computed()`):
`user.store.ts` (session state), `sites.store.ts` (seller-side sites/orders + purchased sites
by project + buyer-side catalog/`requestPublication` — see its own comments for what's
excluded), `stats.store.ts` (DynamicStat widgets, dedupes by `pageKey` on refetch),
`projects.store.ts` (buyer's projects CRUD), `admin.store.ts` (user list + ban/role updates +
dashboard aggregates, new module), `moderation.store.ts` (pending-sites queue + approve/reject,
new module), `favorites.store.ts` (favorited sites + a `Set<number>` of favorite ids as a cheap
overlay for the catalog table's star toggle, new module). Messages deliberately has no store —
`shared/message-chat-modal/` owns its own message list locally since it's short-lived,
one-modal-at-a-time state. Reviews also has no store — `leave-review-modal` calls
`ReviewsApiService` directly and the rating/count it produces is read back as part of `SiteDto`
(refetched via `sites.store.ts`), not tracked as its own state. `notifications.store.ts` (header
bell — unread count + lazily-fetched list, new module): `refreshUnreadCount()` is called from
`NotificationsBootstrapService` on SignalR connect and after every live push, always re-reading
from the server rather than incrementing locally, so a tab that missed an earlier push still ends
up correct. `purchased-site-timeline` (the order-history strip in `order-task-modal`/
`project-details`) deliberately has no store either — it's a single `input()`-driven fetch per
mount, same reasoning as Reviews. Wallet also has no dedicated store — `user.store.ts` grew a
`balance` field on `CurrentUser` plus a `setBalance()` method instead, called after any action
that changes the current user's own balance (see `pages/_NEXT.md`'s wallet paragraph); the
`/wallet` page's own transaction history is a plain `signal()` fetch, same as everywhere else
that doesn't need cross-component sharing.
