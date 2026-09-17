using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfPurchasedSiteRepository(ApplicationDbContext db) : IPurchasedSiteRepository
{
    private IQueryable<PurchasedSite> IncludeAll() =>
        db.PurchasedSites
            .Include(ps => ps.Site).ThenInclude(s => s.Topic)
            .Include(ps => ps.Site).ThenInclude(s => s.Status)
            .Include(ps => ps.Site).ThenInclude(s => s.Country)
            .Include(ps => ps.Site).ThenInclude(s => s.Reviews)
            .Include(ps => ps.Status)
            .Include(ps => ps.Buyer)
            .Include(ps => ps.PaymentSetting)
            .Include(ps => ps.Links)
            .Include(ps => ps.Messages)
            .Include(ps => ps.Review);

    public async Task<(IReadOnlyList<PurchasedSite> Items, int Total)> GetSalesByWebmasterAsync(int webmasterId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = IncludeAll()
            .Where(ps => ps.Site.SellerId == webmasterId)
            .OrderByDescending(ps => ps.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<PurchasedSite?> GetByIdForSellerAsync(int purchasedSiteId, int sellerId, CancellationToken cancellationToken = default) =>
        IncludeAll().FirstOrDefaultAsync(ps => ps.Id == purchasedSiteId && ps.Site.SellerId == sellerId, cancellationToken);

    public Task<PurchasedSite?> GetByIdForBuyerAsync(int purchasedSiteId, int buyerId, CancellationToken cancellationToken = default) =>
        IncludeAll().FirstOrDefaultAsync(ps => ps.Id == purchasedSiteId && ps.BuyerId == buyerId, cancellationToken);

    public async Task<(IReadOnlyList<PurchasedSite> Items, int Total)> GetByProjectAsync(int projectId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = IncludeAll()
            .Where(ps => ps.ProjectId == projectId)
            .OrderByDescending(ps => ps.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<PurchasedSite?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        IncludeAll().FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OrderExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default) =>
        await db.PurchasedSites
            .OrderByDescending(ps => ps.CreatedAt)
            .Select(ps => new OrderExportRow(
                ps.Id,
                ps.Site.Url,
                ps.Buyer.Name,
                ps.Site.Seller.Name,
                ps.FinalPrice,
                ps.Status.Description ?? ps.Status.Name,
                ps.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PurchasedSite purchasedSite, CancellationToken cancellationToken = default) =>
        await db.PurchasedSites.AddAsync(purchasedSite, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<DisputedOrderRow> Items, int Total)> GetDisputedAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.PurchasedSites.Where(ps => ps.IsDisputed).OrderByDescending(ps => ps.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(ps => new DisputedOrderRow(ps.Id, ps.Site.Url, ps.Buyer.Name, ps.Site.Seller.Name, ps.FinalPrice, ps.DisputeReason ?? string.Empty, ps.UpdatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
