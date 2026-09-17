using Application.Common;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DynamicStatsRefresher(ApplicationDbContext db) : IDynamicStatsRefresher
{
    public async Task RefreshProjectCountAsync(CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "project" && s.Position == 1, cancellationToken);
        if (stat == null) return;

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var total = await db.Projects.CountAsync(cancellationToken);
        var beforeLastWeek = await db.Projects.CountAsync(p => p.CreatedAt < weekAgo, cancellationToken);
        var diff = total - beforeLastWeek;

        stat.Value = total.ToString();
        stat.TrendType = diff >= 0 ? "up" : "down";
        stat.TrendPrefix = diff >= 0 ? "+" : string.Empty;
        stat.TrendValue = Math.Abs(diff).ToString();
        stat.TrendText = "за неделю";

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshSiteCountsAsync(int webmasterId, CancellationToken cancellationToken = default)
    {
        var webmasterStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "webmaster" && s.Position == 1, cancellationToken);
        if (webmasterStat != null)
        {
            var monthAgo = DateTime.UtcNow.AddDays(-30);
            var total = await db.Sites.CountAsync(s => s.SellerId == webmasterId && s.IsActive, cancellationToken);
            var before30Days = await db.Sites.CountAsync(
                s => s.SellerId == webmasterId && s.IsActive && s.CreatedAt < monthAgo, cancellationToken);
            var diff = total - before30Days;

            webmasterStat.Value = total.ToString();
            webmasterStat.TrendType = diff >= 0 ? "up" : "down";
            webmasterStat.TrendPrefix = diff >= 0 ? "+" : string.Empty;
            webmasterStat.TrendValue = Math.Abs(diff).ToString();
            webmasterStat.TrendText = "за месяц";
        }

        var optimizatorStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "optimizator" && s.Position == 1, cancellationToken);
        if (optimizatorStat != null)
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var total = await db.Sites.CountAsync(s => s.IsActive, cancellationToken);
            var before7Days = await db.Sites.CountAsync(s => s.IsActive && s.CreatedAt < weekAgo, cancellationToken);
            var diff = total - before7Days;

            optimizatorStat.Value = total.ToString();
            optimizatorStat.TrendType = diff >= 0 ? "up" : "down";
            optimizatorStat.TrendPrefix = diff >= 0 ? "+" : string.Empty;
            optimizatorStat.TrendValue = Math.Abs(diff).ToString();
            optimizatorStat.TrendText = "за неделю";
        }

        var webmasterAvgPriceStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "webmaster" && s.Position == 4, cancellationToken);
        if (webmasterAvgPriceStat != null)
        {
            var avgPrice = await db.Sites.Where(s => s.SellerId == webmasterId && s.IsActive)
                .AverageAsync(s => (decimal?)s.Price, cancellationToken) ?? 0m;
            webmasterAvgPriceStat.Value = avgPrice.ToString("F0");
        }

        var optimizatorAvgPriceStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "optimizator" && s.Position == 3, cancellationToken);
        if (optimizatorAvgPriceStat != null)
        {
            var avgPrice = await db.Sites.Where(s => s.IsActive)
                .AverageAsync(s => (decimal?)s.Price, cancellationToken) ?? 0m;
            optimizatorAvgPriceStat.Value = avgPrice.ToString("F0");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshWebmasterActiveSalesAsync(int webmasterId, CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "webmaster" && s.Position == 2, cancellationToken);
        if (stat == null) return;

        // "application" and "work" — same active-order definition PurchasedSiteObserver uses.
        var activeStatusIds = await db.Statuses
            .Where(s => s.Name == StatusNames.Application || s.Name == StatusNames.Work)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var count = await db.PurchasedSites
            .CountAsync(ps => ps.Site.SellerId == webmasterId && activeStatusIds.Contains(ps.StatusId), cancellationToken);

        stat.Value = count.ToString();
        stat.TrendType = "up";
        stat.TrendText = "актуально";

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshWebmasterRevenueAsync(int webmasterId, CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "webmaster" && s.Position == 3, cancellationToken);
        if (stat == null) return;

        var revenue = await db.PurchasedSites.Where(ps => ps.Site.SellerId == webmasterId)
            .SumAsync(ps => (decimal?)ps.FinalPrice, cancellationToken) ?? 0m;
        stat.Value = revenue.ToString("F0");

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshOptimizatorOrderStatsAsync(CancellationToken cancellationToken = default)
    {
        var activeStatusIds = await db.Statuses
            .Where(s => s.Name == StatusNames.Application || s.Name == StatusNames.Work)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var activeOrdersStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "optimizator" && s.Position == 2, cancellationToken);
        if (activeOrdersStat != null)
        {
            var count = await db.PurchasedSites.CountAsync(ps => activeStatusIds.Contains(ps.StatusId), cancellationToken);
            activeOrdersStat.Value = count.ToString();
        }

        var savingsStat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "optimizator" && s.Position == 4, cancellationToken);
        if (savingsStat != null)
        {
            var savings = await db.PurchasedSites
                .SumAsync(ps => (decimal?)(ps.Site.Price - ps.FinalPrice > 0 ? ps.Site.Price - ps.FinalPrice : 0), cancellationToken) ?? 0m;
            savingsStat.Value = savings.ToString("F0");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshProjectSpendAsync(CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "project" && s.Position == 4, cancellationToken);
        if (stat == null) return;

        var spent = await db.PurchasedSites.SumAsync(ps => (decimal?)ps.FinalPrice, cancellationToken) ?? 0m;
        stat.Value = spent.ToString("F0");

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshProjectWorkStatsAsync(CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "project" && s.Position == 3, cancellationToken);
        if (stat == null) return;

        var workStatusId = await db.Statuses.Where(s => s.Name == StatusNames.Work).Select(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        var count = await db.PurchasedSites.CountAsync(ps => ps.StatusId == workStatusId && !ps.IsPublished, cancellationToken);
        stat.Value = count.ToString();

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshProjectPublishedStatsAsync(CancellationToken cancellationToken = default)
    {
        var stat = await db.DynamicStats
            .FirstOrDefaultAsync(s => s.PageKey == "project" && s.Position == 2, cancellationToken);
        if (stat == null) return;

        var count = await db.PurchasedSites.CountAsync(ps => ps.IsPublished, cancellationToken);
        stat.Value = count.ToString();

        await db.SaveChangesAsync(cancellationToken);
    }
}
