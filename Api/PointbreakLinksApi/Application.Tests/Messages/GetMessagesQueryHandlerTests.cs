using Application.CQRS.Messages.Queries.GetMessages;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Messages;

// Opening a chat is what makes the unread-dot real (see the handler's own comment) — it must
// mark as read only the messages addressed TO the viewer, never the viewer's own sent messages
// (which have no ReadAt concept for the sender), and must skip the save entirely when there's
// nothing to mark, since this runs on every single chat-open.
public class GetMessagesQueryHandlerTests
{
    private const int PurchasedSiteId = 1;
    private const int BuyerId = 20;
    private const int SellerId = 10;

    [Fact]
    public async Task Handle_NonParticipant_ThrowsNotFound()
    {
        var repo = new Mock<IMessageRepository>();
        repo.Setup(r => r.GetOrderForParticipantAsync(PurchasedSiteId, 999, It.IsAny<CancellationToken>())).ReturnsAsync((PurchasedSite?)null);

        var handler = new GetMessagesQueryHandler(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetMessagesQuery(PurchasedSiteId, ViewerId: 999), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnreadMessagesAddressedToViewer_AreMarkedRead()
    {
        var order = TestBuilders.Order(id: PurchasedSiteId, buyerId: BuyerId);
        var incoming = new Message { Id = 1, PurchasedSiteId = PurchasedSiteId, SenderId = SellerId, RecipientId = BuyerId, Text = "hi", ReadAt = null };
        var ownSent = new Message { Id = 2, PurchasedSiteId = PurchasedSiteId, SenderId = BuyerId, RecipientId = SellerId, Text = "hey", ReadAt = null };
        var alreadyRead = new Message { Id = 3, PurchasedSiteId = PurchasedSiteId, SenderId = SellerId, RecipientId = BuyerId, Text = "old", ReadAt = DateTime.UtcNow.AddDays(-1) };

        var repo = new Mock<IMessageRepository>();
        repo.Setup(r => r.GetOrderForParticipantAsync(PurchasedSiteId, BuyerId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByPurchasedSiteAsync(PurchasedSiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Message> { incoming, ownSent, alreadyRead });

        var handler = new GetMessagesQueryHandler(repo.Object);
        await handler.Handle(new GetMessagesQuery(PurchasedSiteId, ViewerId: BuyerId), CancellationToken.None);

        Assert.NotNull(incoming.ReadAt); // marked read — addressed to the viewer and was unread
        Assert.Null(ownSent.ReadAt); // untouched — the viewer's own sent message, not theirs to "read"
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoUnreadMessages_DoesNotSave()
    {
        var order = TestBuilders.Order(id: PurchasedSiteId, buyerId: BuyerId);
        var ownSent = new Message { Id = 2, PurchasedSiteId = PurchasedSiteId, SenderId = BuyerId, RecipientId = SellerId, Text = "hey", ReadAt = null };

        var repo = new Mock<IMessageRepository>();
        repo.Setup(r => r.GetOrderForParticipantAsync(PurchasedSiteId, BuyerId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByPurchasedSiteAsync(PurchasedSiteId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Message> { ownSent });

        var handler = new GetMessagesQueryHandler(repo.Object);
        await handler.Handle(new GetMessagesQuery(PurchasedSiteId, ViewerId: BuyerId), CancellationToken.None);

        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
