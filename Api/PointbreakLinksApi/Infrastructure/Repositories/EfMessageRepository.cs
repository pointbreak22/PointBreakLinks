using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfMessageRepository(ApplicationDbContext db) : IMessageRepository
{
    public Task<PurchasedSite?> GetOrderForParticipantAsync(int purchasedSiteId, int userId, CancellationToken cancellationToken = default) =>
        db.PurchasedSites
            .AsNoTracking()
            .Include(ps => ps.Site).ThenInclude(s => s.Seller)
            .Include(ps => ps.Buyer)
            .FirstOrDefaultAsync(ps => ps.Id == purchasedSiteId && (ps.BuyerId == userId || ps.Site.SellerId == userId), cancellationToken);

    // NOT AsNoTracking: GetMessagesQueryHandler mutates ReadAt on the returned messages (marks
    // the viewer's unread messages read) and calls SaveChangesAsync — this must stay tracked.
    public Task<List<Message>> GetByPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default) =>
        db.Messages.Where(m => m.PurchasedSiteId == purchasedSiteId).OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken);

    public Task<Message?> GetByIdAsync(int messageId, CancellationToken cancellationToken = default) =>
        db.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default) =>
        await db.Messages.AddAsync(message, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<List<PurchasedSite>> GetConversationsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.PurchasedSites
            .AsNoTracking()
            .Include(ps => ps.Site).ThenInclude(s => s.Seller)
            .Include(ps => ps.Buyer)
            .Include(ps => ps.Messages)
            .Where(ps => (ps.BuyerId == userId || ps.Site.SellerId == userId) && ps.Messages.Count > 0)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Messages.CountAsync(m => m.RecipientId == userId && m.ReadAt == null, cancellationToken);
}
