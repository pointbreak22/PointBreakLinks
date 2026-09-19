using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Sites.Commands.AcceptOrder;

// Wallet enforcement (not in FOXLinks — see Application/CQRS/Wallet): the seller is credited
// here, at accept, not at order creation — crediting at creation would pay a seller for an
// order they haven't even agreed to fulfil yet. The buyer was already debited at creation (see
// RequestPublicationCommandHandler); there's no reject/cancel path in this app that would need
// to reverse either side, so this stays a one-way transfer.
public class AcceptOrderCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    IDynamicStatsRefresher statsRefresher,
    INotificationPusher notificationPusher,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    INotificationPreferenceRepository notificationPreferenceRepository,
    ILogger<AcceptOrderCommandHandler> logger)
    : IRequestHandler<AcceptOrderCommand, PurchasedSiteDto>
{
    // Matches the fixed ids StatusConfiguration.HasData seeds — see Domain/Constants/StatusNames.cs.
    private const int WorkStatusId = 4;

    public async Task<PurchasedSiteDto> Handle(AcceptOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.WebmasterId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        order.StatusId = WorkStatusId;
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

        var wallet = await walletRepository.GetOrCreateAsync(request.WebmasterId, cancellationToken);
        wallet.Balance += order.FinalPrice;
        await walletRepository.SaveChangesAsync(cancellationToken);
        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = request.WebmasterId,
                Type = BalanceTransactionType.Sale,
                Amount = order.FinalPrice,
                Description = $"Продажа размещения на {order.Site.Url}",
                PurchasedSiteId = order.Id,
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} accepted by seller {SellerId}; {Amount} credited to wallet for {SiteUrl}.",
            order.Id, request.WebmasterId, order.FinalPrice, order.Site.Url);

        await statsRefresher.RefreshWebmasterActiveSalesAsync(request.WebmasterId, cancellationToken);
        await statsRefresher.RefreshProjectWorkStatsAsync(cancellationToken);
        await notificationPusher.NotifyOrderAcceptedAsync(order.BuyerId, order.Site.Url, cancellationToken);

        await notificationRepository.AddAsync(
            new Notification { UserId = order.BuyerId, Message = $"Ваш заказ на {order.Site.Url} принят в работу" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        var buyer = await userRepository.GetByIdWithRolesAndProjectsAsync(order.BuyerId, cancellationToken);
        var buyerPreference = await notificationPreferenceRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
        if (buyer != null && buyerPreference.EmailOnOrderUpdates)
        {
            await emailSender.SendAsync(
                buyer.Email,
                "Ваш заказ принят в работу",
                $"""
                 <p>Здравствуйте, {buyer.Name}!</p>
                 <p>Продавец принял ваш заказ на площадку <b>{order.Site.Url}</b> в работу.</p>
                 """,
                cancellationToken);
        }

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Заказ принят продавцом в работу" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        // Re-fetch so the Status navigation reflects the new StatusId (EF doesn't
        // auto-refresh a loaded reference navigation just because its FK changed).
        var updated = await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.WebmasterId, cancellationToken)
                      ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);
        return PurchasedSiteDto.FromEntity(updated, request.WebmasterId);
    }
}
