using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Sites.Commands.CancelOrder;

// Buyer-side half of the order-cancel feature (DeclineOrderCommandHandler is the seller-side
// half) — added this session because there was previously no way out of "application" status
// short of the seller eventually accepting: RequestPublicationCommandHandler debits the buyer
// immediately, and nothing ever reversed that if the order just sat there forever. Deliberately
// scoped to "application" only — once a seller has accepted ("work"), reversing a payout that
// may already be spent gets a lot messier, and isn't what was asked for here.
public class CancelOrderCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    ISiteRepository siteRepository,
    IDynamicStatsRefresher statsRefresher,
    INotificationPusher notificationPusher,
    INotificationRepository notificationRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    INotificationPreferenceRepository notificationPreferenceRepository,
    ILogger<CancelOrderCommandHandler> logger)
    : IRequestHandler<CancelOrderCommand, PurchasedSiteDto>
{
    private const int ApplicationStatusId = 1;
    private const int CancelledStatusId = 7;

    public async Task<PurchasedSiteDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.BuyerId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        if (order.StatusId != ApplicationStatusId)
        {
            throw new ConflictException("Отменить можно только заказ в статусе «Заявка».");
        }

        order.StatusId = CancelledStatusId;
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

        var wallet = await walletRepository.GetOrCreateAsync(request.BuyerId, cancellationToken);
        wallet.Balance += order.FinalPrice;
        await walletRepository.SaveChangesAsync(cancellationToken);
        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = request.BuyerId,
                Type = BalanceTransactionType.Refund,
                Amount = order.FinalPrice,
                Description = $"Возврат за отменённый заказ на площадку {order.Site.Url}",
                PurchasedSiteId = order.Id,
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} cancelled by buyer {BuyerId}; {Amount} refunded for {SiteUrl}.",
            order.Id, request.BuyerId, order.FinalPrice, order.Site.Url);

        order.Site.SoldCount = Math.Max(0, order.Site.SoldCount - 1);
        await siteRepository.SaveChangesAsync(cancellationToken);

        await statsRefresher.RefreshWebmasterActiveSalesAsync(order.Site.SellerId, cancellationToken);
        await statsRefresher.RefreshOptimizatorOrderStatsAsync(cancellationToken);
        await statsRefresher.RefreshProjectSpendAsync(cancellationToken);

        await notificationPusher.NotifyOrderCancelledAsync(order.Site.SellerId, order.Site.Url, cancellationToken);
        await notificationRepository.AddAsync(
            new Notification { UserId = order.Site.SellerId, Message = $"Заказ на площадку {order.Site.Url} отменён покупателем" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Заказ отменён покупателем" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        var seller = await userRepository.GetByIdWithRolesAndProjectsAsync(order.Site.SellerId, cancellationToken);
        var sellerPreference = await notificationPreferenceRepository.GetOrCreateAsync(order.Site.SellerId, cancellationToken);
        if (seller != null && sellerPreference.EmailOnOrderUpdates)
        {
            await emailSender.SendAsync(
                seller.Email,
                $"Заказ на площадку {order.Site.Url} отменён",
                $"""
                 <p>Здравствуйте, {seller.Name}!</p>
                 <p>Покупатель отменил заказ на размещение на площадке <b>{order.Site.Url}</b> до принятия его в работу.</p>
                 """,
                cancellationToken);
        }

        var updated = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.BuyerId, cancellationToken)
                      ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);
        return PurchasedSiteDto.FromEntity(updated, request.BuyerId);
    }
}
