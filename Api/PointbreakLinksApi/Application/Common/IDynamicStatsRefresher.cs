namespace Application.Common;

// Ported from FOXLinks' Observers (ProjectObserver, SiteObserver, PurchasedSiteObserver) —
// Eloquent model-event hooks that recompute a DynamicStat row's value/trend whenever the
// underlying data changes, so the SmartGrid widgets stay live instead of frozen at whatever
// the seeder set them to. There's no MediatR domain-event plumbing here on purpose: these are
// synchronous, single-row recomputations called directly from the command handler that just
// changed the underlying data (CreateProjectCommandHandler, RequestPublicationCommandHandler,
// ...) — an event bus would be overkill for a same-transaction side effect this small.
//
// FOXLinks' Observers only ever touch position 1 on all three pages plus position 2 on
// "webmaster" — positions 3/4 sit at the seeder's "0" forever in the source, a decorative dead
// card. This port computes all twelve for real instead (see DynamicStatConfiguration's seed
// comment). Some rows are per-seller (matching FOXLinks' existing webmasterId-scoped calls,
// including its shared-row "last writer wins" quirk when two sellers act around the same time —
// inherited, not introduced here); others are page-wide totals, matching how position 1 already
// works unscoped on "optimizator"/"project".
public interface IDynamicStatsRefresher
{
    Task RefreshProjectCountAsync(CancellationToken cancellationToken = default);

    // Site count widgets: webmaster's own active-listing count (page_key "webmaster",
    // position 1) and the system-wide active-listing count shown to buyers (page_key
    // "optimizator", position 1) — both change together whenever any site is
    // created/deactivated, mirrors SiteObserver's updateWebmasterStats + updateOptimizatorStats.
    // Also refreshes the average-listing-price widgets that change on exactly the same trigger:
    // webmaster's own average (page_key "webmaster", position 4) and the system-wide average
    // shown to buyers (page_key "optimizator", position 3).
    Task RefreshSiteCountsAsync(int webmasterId, CancellationToken cancellationToken = default);

    // Webmaster's active-orders count (page_key "webmaster", position 2) — mirrors
    // PurchasedSiteObserver::refreshSalesCount.
    Task RefreshWebmasterActiveSalesAsync(int webmasterId, CancellationToken cancellationToken = default);

    // Webmaster's total revenue (page_key "webmaster", position 3) — sum of FinalPrice across
    // this seller's orders, any status (same "no status filter" choice AdminDashboardDto's
    // TotalRevenue already makes, for consistency). Only changes when a new order is created —
    // accepting/rejecting an existing order doesn't change the sum of a fixed set of rows.
    Task RefreshWebmasterRevenueAsync(int webmasterId, CancellationToken cancellationToken = default);

    // System-wide buyer-facing order stats (page_key "optimizator"): active-orders count
    // (position 2, status in application/work — mirrors position 2's own definition on
    // "webmaster" but unscoped) and total savings (position 4: sum over every order of
    // max(0, Site.Price - PurchasedSite.FinalPrice) — a real, currently-usually-zero formula
    // given FinalPrice's insurance/urgency multipliers push it above Price, not below; not
    // fabricated, just often uninteresting with today's pricing model). Both are fixed once an
    // order is created, so only that one trigger point calls this.
    Task RefreshOptimizatorOrderStatsAsync(CancellationToken cancellationToken = default);

    // System-wide buyer-facing spend total (page_key "project", position 4) — sum of FinalPrice
    // across every order, platform-wide. Fixed at order creation, same reasoning as
    // RefreshWebmasterRevenueAsync.
    Task RefreshProjectSpendAsync(CancellationToken cancellationToken = default);

    // System-wide "links currently in progress" (page_key "project", position 3) — count of
    // orders with status "work" that aren't yet confirmed published. Changes when an order is
    // accepted (enters this set) and when its publication is confirmed (leaves it).
    Task RefreshProjectWorkStatsAsync(CancellationToken cancellationToken = default);

    // System-wide "links actually posted" (page_key "project", position 2) — count of orders
    // with IsPublished = true. Only changes when a publication is confirmed.
    Task RefreshProjectPublishedStatsAsync(CancellationToken cancellationToken = default);
}
