using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfAnalyticsRepository(ApplicationDbContext db) : IAnalyticsRepository
{
    public async Task<AnalyticsStats> GetStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sixMonthsAgo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var totalSpent = await db.PurchasedSites.Where(ps => ps.Project.UserId == userId)
            .SumAsync(ps => (decimal?)ps.FinalPrice, cancellationToken) ?? 0m;

        var totalEarned = await db.PurchasedSites.Where(ps => ps.Site.SellerId == userId)
            .SumAsync(ps => (decimal?)ps.FinalPrice, cancellationToken) ?? 0m;

        var activeProjects = await db.Projects.CountAsync(p => p.UserId == userId, cancellationToken);
        var activeSites = await db.Sites.CountAsync(s => s.SellerId == userId && s.IsActive, cancellationToken);

        var spendRows = await db.PurchasedSites.Where(ps => ps.Project.UserId == userId && ps.CreatedAt >= sixMonthsAgo)
            .Select(ps => new { ps.CreatedAt, ps.FinalPrice })
            .ToListAsync(cancellationToken);
        var spendByMonth = BucketByMonth(spendRows.Select(r => (r.CreatedAt, r.FinalPrice)), sixMonthsAgo);

        var earnedRows = await db.PurchasedSites.Where(ps => ps.Site.SellerId == userId && ps.CreatedAt >= sixMonthsAgo)
            .Select(ps => new { ps.CreatedAt, ps.FinalPrice })
            .ToListAsync(cancellationToken);
        var earnedByMonth = BucketByMonth(earnedRows.Select(r => (r.CreatedAt, r.FinalPrice)), sixMonthsAgo);

        var ordersByStatus = await db.PurchasedSites.Where(ps => ps.Project.UserId == userId)
            .GroupBy(ps => new { ps.Status.Name, ps.Status.Description })
            .Select(g => new AnalyticsStatusBreakdown(g.Key.Name, g.Key.Description, g.Count()))
            .ToListAsync(cancellationToken);

        var projectRows = await db.Projects.Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Spent = p.PurchasedSites.Sum(ps => (decimal?)ps.FinalPrice) ?? 0m,
                OrdersCount = p.PurchasedSites.Count,
                LinksPosted = p.PurchasedSites.Count(ps => ps.IsPublished),
            })
            .ToListAsync(cancellationToken);

        return new AnalyticsStats(
            totalSpent,
            totalEarned,
            activeProjects,
            activeSites,
            spendByMonth,
            earnedByMonth,
            ordersByStatus,
            projectRows.Select(p => new AnalyticsProjectSummary(p.Id, p.Name, p.Spent, p.OrdersCount, p.LinksPosted)).ToList());
    }

    private static List<AnalyticsMonthlyPoint> BucketByMonth(IEnumerable<(DateTime CreatedAt, decimal FinalPrice)> rows, DateTime start)
    {
        var buckets = Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .ToDictionary(d => (d.Year, d.Month), _ => 0m);

        foreach (var (createdAt, finalPrice) in rows)
        {
            var key = (createdAt.Year, createdAt.Month);
            if (buckets.ContainsKey(key))
            {
                buckets[key] += finalPrice;
            }
        }

        return buckets
            .OrderBy(kv => kv.Key.Year).ThenBy(kv => kv.Key.Month)
            .Select(kv => new AnalyticsMonthlyPoint(kv.Key.Year, kv.Key.Month, kv.Value))
            .ToList();
    }
}
