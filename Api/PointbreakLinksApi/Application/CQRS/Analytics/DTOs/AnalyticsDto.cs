namespace Application.CQRS.Analytics.DTOs;

public record MonthlyPointDto(string Month, decimal Value);

public record StatusBreakdownDto(string Status, string Description, int Count);

public record ProjectSummaryDto(int Id, string Name, decimal Spent, int OrdersCount, int LinksPosted);

// Real aggregation over this user's own Project/PurchasedSite/Site rows — no per-keyword SERP
// data (visibility %, CTR, average position) exists anywhere in this app's domain, so unlike
// FOXLinks' analytics.vue (100% hardcoded Chart.js arrays, see PROJECT_MAP.md), this only shows
// what can actually be computed from real orders and listings.
public record AnalyticsDto(
    decimal TotalSpent,
    decimal TotalEarned,
    int ActiveProjects,
    int ActiveSites,
    IReadOnlyList<MonthlyPointDto> SpendByMonth,
    IReadOnlyList<MonthlyPointDto> EarnedByMonth,
    IReadOnlyList<StatusBreakdownDto> OrdersByStatus,
    IReadOnlyList<ProjectSummaryDto> Projects);
