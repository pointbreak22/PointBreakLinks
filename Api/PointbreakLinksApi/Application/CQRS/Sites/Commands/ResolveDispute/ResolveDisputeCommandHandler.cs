using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Sites.Commands.ResolveDispute;

// Admin-only resolution for a dispute opened by OpenDisputeCommandHandler. Two outcomes:
// refund the buyer (clawing back the seller's already-paid credit, which can legitimately push
// their Wallet.Balance negative — see BalanceTransactionType.DisputeChargeback's comment) or
// release the dispute with no money movement (seller keeps what they were paid, order stays
// "work" — the buyer is expected to keep working it out via the order chat / wait for
// publication as normal).
public class ResolveDisputeCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    INotificationRepository notificationRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    INotificationPreferenceRepository notificationPreferenceRepository,
    ILogger<ResolveDisputeCommandHandler> logger)
    : IRequestHandler<ResolveDisputeCommand, PurchasedSiteDto>
{
    private const int CancelledStatusId = 7;

    public async Task<PurchasedSiteDto> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdAsync(request.PurchasedSiteId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        if (!order.IsDisputed)
        {
            throw new ConflictException("По этому заказу нет открытого спора.");
        }

        order.IsDisputed = false;

        var seller = await userRepository.GetByIdWithRolesAndProjectsAsync(order.Site.SellerId, cancellationToken);

        if (request.RefundBuyer)
        {
            order.StatusId = CancelledStatusId;
            await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

            var sellerWallet = await walletRepository.GetOrCreateAsync(order.Site.SellerId, cancellationToken);
            sellerWallet.Balance -= order.FinalPrice;
            await walletRepository.SaveChangesAsync(cancellationToken);
            await transactionRepository.AddAsync(
                new BalanceTransaction
                {
                    UserId = order.Site.SellerId,
                    Type = BalanceTransactionType.DisputeChargeback,
                    Amount = -order.FinalPrice,
                    Description = $"Списание по итогам спора — заказ на площадку {order.Site.Url}",
                    PurchasedSiteId = order.Id,
                },
                cancellationToken);

            var buyerWallet = await walletRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
            buyerWallet.Balance += order.FinalPrice;
            await walletRepository.SaveChangesAsync(cancellationToken);
            await transactionRepository.AddAsync(
                new BalanceTransaction
                {
                    UserId = order.BuyerId,
                    Type = BalanceTransactionType.Refund,
                    Amount = order.FinalPrice,
                    Description = $"Возврат по итогам спора — заказ на площадку {order.Site.Url}",
                    PurchasedSiteId = order.Id,
                },
                cancellationToken);
            await transactionRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Dispute on order {OrderId} resolved in buyer's favor; {Amount} charged back from seller {SellerId} and refunded to buyer {BuyerId}.",
                order.Id, order.FinalPrice, order.Site.SellerId, order.BuyerId);

            await purchasedSiteEventRepository.AddAsync(
                new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Спор решён администратором в пользу покупателя — средства возвращены" },
                cancellationToken);

            await notificationRepository.AddAsync(
                new Notification { UserId = order.BuyerId, Message = $"Спор по заказу на {order.Site.Url} решён в вашу пользу, средства возвращены" },
                cancellationToken);
            if (seller != null)
            {
                await notificationRepository.AddAsync(
                    new Notification { UserId = order.Site.SellerId, Message = $"Спор по заказу на {order.Site.Url} решён в пользу покупателя" },
                    cancellationToken);

                var sellerPreference = await notificationPreferenceRepository.GetOrCreateAsync(order.Site.SellerId, cancellationToken);
                if (sellerPreference.EmailOnDisputeUpdates)
                {
                    await emailSender.SendAsync(
                        seller.Email,
                        $"Спор по заказу на {order.Site.Url} решён не в вашу пользу",
                        $"""
                         <p>Здравствуйте, {seller.Name}!</p>
                         <p>Администратор рассмотрел спор по заказу на площадке <b>{order.Site.Url}</b> и вернул оплату покупателю.</p>
                         """,
                        cancellationToken);
                }
            }

            var buyerPreferenceRefund = await notificationPreferenceRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
            if (buyerPreferenceRefund.EmailOnDisputeUpdates)
            {
                await emailSender.SendAsync(
                    order.Buyer.Email,
                    $"Спор по заказу на {order.Site.Url} решён в вашу пользу",
                    $"""
                     <p>Здравствуйте, {order.Buyer.Name}!</p>
                     <p>Администратор рассмотрел ваш спор по заказу на площадке <b>{order.Site.Url}</b> и вернул оплату на ваш баланс.</p>
                     """,
                    cancellationToken);
            }
        }
        else
        {
            await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Dispute on order {OrderId} resolved in seller's favor, no funds moved.", order.Id);

            await purchasedSiteEventRepository.AddAsync(
                new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Спор решён администратором в пользу продавца" },
                cancellationToken);

            await notificationRepository.AddAsync(
                new Notification { UserId = order.BuyerId, Message = $"Спор по заказу на {order.Site.Url} решён в пользу продавца" },
                cancellationToken);

            var buyerPreference = await notificationPreferenceRepository.GetOrCreateAsync(order.BuyerId, cancellationToken);
            if (buyerPreference.EmailOnDisputeUpdates)
            {
                await emailSender.SendAsync(
                    order.Buyer.Email,
                    $"Спор по заказу на {order.Site.Url} решён не в вашу пользу",
                    $"""
                     <p>Здравствуйте, {order.Buyer.Name}!</p>
                     <p>Администратор рассмотрел ваш спор по заказу на площадке <b>{order.Site.Url}</b> и оставил заказ в силе.</p>
                     """,
                    cancellationToken);
            }
        }

        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        var updated = await purchasedSiteRepository.GetByIdAsync(request.PurchasedSiteId, cancellationToken)
                      ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);
        return PurchasedSiteDto.FromEntity(updated, order.BuyerId);
    }
}
