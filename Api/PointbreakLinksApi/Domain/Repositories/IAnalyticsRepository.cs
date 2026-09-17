namespace Domain.Repositories;

public record AnalyticsMonthlyPoint(int Year, int Month, decimal Value);

public record AnalyticsStatusBreakdown(string StatusName, string? StatusDescription, int Count);

public record AnalyticsProjectSummary(int Id, string Name, decimal Spent, int OrdersCount, int LinksPosted);

// Same "plain aggregation at read time" reasoning as IAdminDashboardRepository — this is a
// per-user cross-cutting summary (buyer spend + seller earnings + per-project breakdown), not a
// single cached counter a specific command handler could keep fresh, so it's computed fresh on
// every read instead of routed through IDynamicStatsRefresher/DynamicStat.
public record AnalyticsStats(
    decimal TotalSpent,
    decimal TotalEarned,
    int ActiveProjects,
    int ActiveSites,
    IReadOnlyList<AnalyticsMonthlyPoint> SpendByMonth,
    IReadOnlyList<AnalyticsMonthlyPoint> EarnedByMonth,
    IReadOnlyList<AnalyticsStatusBreakdown> OrdersByStatus,
    IReadOnlyList<AnalyticsProjectSummary> Projects);

public interface IAnalyticsRepository
{
    Task<AnalyticsStats> GetStatsAsync(int userId, CancellationToken cancellationToken = default);
}
