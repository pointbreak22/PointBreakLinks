using Application.CQRS.Sites.Commands.OpenDispute;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sites;

// Regression coverage for the exact gate OpenDisputeCommandHandler enforces: a dispute can only
// be opened on an order in "work" status (id 4) — see the handler's own comment on why
// CancelOrderCommandHandler can't cover this case. This rule is easy to accidentally loosen
// during an unrelated refactor since nothing else in the codebase re-checks it.
public class OpenDisputeCommandHandlerTests
{
    private static (OpenDisputeCommandHandler Handler, Mock<IPurchasedSiteRepository> Repo) CreateHandler(PurchasedSite order)
    {
        var repo = new Mock<IPurchasedSiteRepository>();
        repo.Setup(r => r.GetByIdForBuyerAsync(order.Id, order.BuyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var eventRepo = new Mock<IPurchasedSiteEventRepository>();
        var handler = new OpenDisputeCommandHandler(repo.Object, eventRepo.Object);
        return (handler, repo);
    }

    [Theory]
    [InlineData(1, "application")]
    [InlineData(2, "paid")]
    [InlineData(6, "active")]
    public async Task Handle_OrderNotInWorkStatus_ThrowsConflict(int statusId, string statusName)
    {
        var order = TestBuilders.Order(statusId: statusId, statusName: statusName);
        var (handler, _) = CreateHandler(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new OpenDisputeCommand(order.Id, order.BuyerId, "Продавец не выходит на связь"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyDisputed_ThrowsConflict()
    {
        var order = TestBuilders.Order(statusId: 4, statusName: "work");
        order.IsDisputed = true;
        var (handler, _) = CreateHandler(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new OpenDisputeCommand(order.Id, order.BuyerId, "Ещё одна причина"), CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_BlankReason_ThrowsConflict(string reason)
    {
        var order = TestBuilders.Order(statusId: 4, statusName: "work");
        var (handler, _) = CreateHandler(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new OpenDisputeCommand(order.Id, order.BuyerId, reason), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidDisputeOnWorkOrder_SetsFlagsAndPersists()
    {
        var order = TestBuilders.Order(statusId: 4, statusName: "work");
        var (handler, repo) = CreateHandler(order);

        var result = await handler.Handle(new OpenDisputeCommand(order.Id, order.BuyerId, "Продавец не выходит на связь"), CancellationToken.None);

        Assert.True(order.IsDisputed);
        Assert.Equal("Продавец не выходит на связь", order.DisputeReason);
        Assert.True(result.IsDisputed);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
