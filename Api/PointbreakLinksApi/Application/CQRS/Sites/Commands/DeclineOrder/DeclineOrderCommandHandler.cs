using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Sites.Commands.DeclineOrder;

// Seller-side half of the order-cancel feature — see CancelOrderCommandHandler's comment for
// why this only applies to "application" status. Mirrors AcceptOrderCommandHandler's shape
// (same ownership check via GetByIdForSellerAsync) but refunds the buyer instead of crediting
// the seller, since nothing was ever earned here.
public class DeclineOrderCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    ISiteRepository siteRepository,
    IDynamicStatsRefresher statsRefresher,
    INotificationPusher notificationPusher,
    INotificationRepository notificationRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    IEmailSender emailSender,
    INotificationPreferenceRepository notificationPreferenceRepository,
    ILogger<DeclineOrderCommandHandler> logger)
    : IRequestHandler<DeclineOrderCommand, PurchasedSiteDto>
{
    private const int ApplicationStatusId = 1;
    private const int CancelledStatusId = 7;

    public async Task<PurchasedSiteDto> Handle(DeclineOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.WebmasterId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        if (order.StatusId != ApplicationStatusId)
        {
            throw new ConflictException("Отклонить можно только заказ в статусе «Заявка».");
        }

        order.StatusId = CancelledStatusId;
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

        var wallet = await walletRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
        wallet.Balance += order.FinalPrice;
        await walletRepository.SaveChangesAsync(cancellationToken);
        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = order.BuyerId,
                Type = BalanceTransactionType.Refund,
                Amount = order.FinalPrice,
                Description = $"Возврат за отклонённый заказ на площадку {order.Site.Url}",
                PurchasedSiteId = order.Id,
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} declined by seller {SellerId}; {Amount} refunded for {SiteUrl}.",
            order.Id, request.WebmasterId, order.FinalPrice, order.Site.Url);

        order.Site.SoldCount = Math.Max(0, order.Site.SoldCount - 1);
        await siteRepository.SaveChangesAsync(cancellationToken);

        await statsRefresher.RefreshWebmasterActiveSalesAsync(request.WebmasterId, cancellationToken);
        await statsRefresher.RefreshOptimizatorOrderStatsAsync(cancellationToken);
        await statsRefresher.RefreshProjectSpendAsync(cancellationToken);

        await notificationPusher.NotifyOrderDeclinedAsync(order.BuyerId, order.Site.Url, cancellationToken);
        await notificationRepository.AddAsync(
            new Notification { UserId = order.BuyerId, Message = $"Заказ на площадку {order.Site.Url} отклонён продавцом" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Заказ отклонён продавцом" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        var buyerPreference = await notificationPreferenceRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
        if (buyerPreference.EmailOnOrderUpdates)
        {
            await emailSender.SendAsync(
                order.Buyer.Email,
                $"Заказ на площадку {order.Site.Url} отклонён",
                $"""
                 <p>Здравствуйте, {order.Buyer.Name}!</p>
                 <p>Продавец отклонил ваш заказ на размещение на площадке <b>{order.Site.Url}</b>. Средства возвращены на баланс.</p>
                 """,
                cancellationToken);
        }

        var updated = await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.WebmasterId, cancellationToken)
                      ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);
        return PurchasedSiteDto.FromEntity(updated, request.WebmasterId);
    }
}
