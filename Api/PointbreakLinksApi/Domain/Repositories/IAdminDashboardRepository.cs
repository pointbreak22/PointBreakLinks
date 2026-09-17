namespace Domain.Repositories;

public record AdminDashboardStats(
    int TotalUsers,
    int TotalProjects,
    int TotalActiveSites,
    int TotalOrders,
    decimal TotalRevenue,
    int PendingModerationCount,
    IReadOnlyList<AdminRoleCount> UsersByRole,
    IReadOnlyList<AdminRecentUser> RecentUsers,
    IReadOnlyList<AdminRecentOrder> RecentOrders);

public record AdminRoleCount(string Name, string DisplayName, int Count);
public record AdminRecentUser(int Id, string Name, string Email, DateTime CreatedAt);
public record AdminRecentOrder(int Id, string SiteUrl, string BuyerName, decimal FinalPrice, string StatusDescription, DateTime CreatedAt);

// Cross-cutting read, deliberately not routed through IDynamicStatsRefresher/DynamicStat like
// every other page's SmartGrid widgets: those are cached counters kept fresh by hooking each
// specific mutation that changes them (site created, order placed, ...). A platform-wide
// "everything" summary would need that same hook sprinkled across nearly every command handler
// in the app just to stay accurate — a plain aggregation query at read time is both simpler and
// actually correct (SmartGrid's cached counters are only as fresh as the last write that
// happened to trigger a refresh; this is always current).
public interface IAdminDashboardRepository
{
    Task<AdminDashboardStats> GetStatsAsync(CancellationToken cancellationToken = default);
}
