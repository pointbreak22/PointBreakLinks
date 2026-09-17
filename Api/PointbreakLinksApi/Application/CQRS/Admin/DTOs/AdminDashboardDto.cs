namespace Application.CQRS.Admin.DTOs;

// Mirrors the 4 stat cards on FOXLinks' admin-dashboard.vue, minus the ones with no honest way
// to compute: that page also hardcoded "Комиссия системы" (a commission split), but
// PurchasedSite only stores the buyer's final price — the client-computed total that already
// has the 10% commission baked in — with no separate record of the pre-commission base price at
// order time, so splitting it back out here would be a guess dressed up as a real number.
//
// UsersByRole/RecentUsers/RecentOrders added later to fill out the dashboard beyond the 5 raw
// counters — all still plain aggregation/projection queries, no invented numbers.
public record AdminDashboardDto(
    int TotalUsers,
    int TotalProjects,
    int TotalActiveSites,
    int TotalOrders,
    decimal TotalRevenue,
    int PendingModerationCount,
    IReadOnlyList<AdminRoleCountDto> UsersByRole,
    IReadOnlyList<AdminRecentUserDto> RecentUsers,
    IReadOnlyList<AdminRecentOrderDto> RecentOrders);

public record AdminRoleCountDto(string Name, string DisplayName, int Count);
public record AdminRecentUserDto(int Id, string Name, string Email, DateTime CreatedAt);
public record AdminRecentOrderDto(int Id, string SiteUrl, string BuyerName, decimal FinalPrice, string StatusDescription, DateTime CreatedAt);
