using Domain.Entities;

namespace Domain.Repositories;

public interface IMessageRepository
{
    // The order itself (not just the message) is fetched so handlers can verify the caller is
    // either its buyer or its seller before allowing read/send — chat is scoped per order.
    Task<PurchasedSite?> GetOrderForParticipantAsync(int purchasedSiteId, int userId, CancellationToken cancellationToken = default);

    Task<List<Message>> GetByPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default);

    // Backs the attachment download endpoint — callers still verify participancy via
    // GetOrderForParticipantAsync before trusting anything read here.
    Task<Message?> GetByIdAsync(int messageId, CancellationToken cancellationToken = default);

    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Backs the messages inbox: every order the user is a party to (buyer or seller) that has
    // at least one message — each such order is exactly one "conversation" in the inbox.
    Task<List<PurchasedSite>> GetConversationsForUserAsync(int userId, CancellationToken cancellationToken = default);

    // A direct count, not a sum over GetConversationsForUserAsync — the header badge needs this
    // on every login/push without paying for the full conversations query each time.
    Task<int> CountUnreadForUserAsync(int userId, CancellationToken cancellationToken = default);
}
