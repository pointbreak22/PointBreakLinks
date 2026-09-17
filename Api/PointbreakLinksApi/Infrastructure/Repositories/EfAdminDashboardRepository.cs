using Domain.Constants;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfAdminDashboardRepository(ApplicationDbContext db) : IAdminDashboardRepository
{
    public async Task<AdminDashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await db.Users.CountAsync(cancellationToken);
        var totalProjects = await db.Projects.CountAsync(cancellationToken);
        var totalActiveSites = await db.Sites.CountAsync(s => s.IsActive, cancellationToken);
        var totalOrders = await db.PurchasedSites.CountAsync(cancellationToken);
        var totalRevenue = await db.PurchasedSites.SumAsync(p => (decimal?)p.FinalPrice, cancellationToken) ?? 0m;
        var pendingModerationCount = await db.Sites.CountAsync(s => s.Status.Name == StatusNames.Moderation, cancellationToken);

        var usersByRole = await db.Roles
            .OrderByDescending(r => r.Users.Count)
            .Select(r => new AdminRoleCount(r.Name, r.DisplayName ?? r.Name, r.Users.Count))
            .ToListAsync(cancellationToken);

        var recentUsers = await db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new AdminRecentUser(u.Id, u.Name, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);

        var recentOrders = await db.PurchasedSites
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new AdminRecentOrder(p.Id, p.Site.Url, p.Buyer.Name, p.FinalPrice, p.Status.Description ?? p.Status.Name, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminDashboardStats(
            totalUsers,
            totalProjects,
            totalActiveSites,
            totalOrders,
            totalRevenue,
            pendingModerationCount,
            usersByRole,
            recentUsers,
            recentOrders);
    }
}
