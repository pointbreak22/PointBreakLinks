using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.RequestPublication;

// Mirrors FOXLinks' SiteRepository::applyPublicationRequest — PaymentSetting dedup, a new
// PurchasedSite under status "application", the optional Link rows, and Site.SoldCount++.
// Deliberately does not replicate the source's `$site->user_id_buyer` ownership check: that
// column doesn't exist on the sites table (confirmed against the original migration), so the
// check in FOXLinks could never actually pass — it's dead code, not a rule to port.
// Wallet enforcement (not in FOXLinks — see Application/CQRS/Wallet): the buyer's balance is
// checked and debited here, at order creation, not at accept/publish — FinalPrice is locked in
// the moment the order exists, and there's no reject/cancel path anywhere in this app that
// would need to reverse the debit (see AcceptOrderCommandHandler's comment on why crediting the
// seller waits until they accept instead).
public class RequestPublicationCommandHandler(
    ISiteRepository siteRepository,
    IProjectRepository projectRepository,
    IPurchasedSiteRepository purchasedSiteRepository,
    IPaymentSettingRepository paymentSettingRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    IDynamicStatsRefresher statsRefresher,
    INotificationPusher notificationPusher,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    INotificationPreferenceRepository notificationPreferenceRepository)
    : IRequestHandler<RequestPublicationCommand, PurchasedSiteDto>
{
    // Matches the fixed id StatusConfiguration.HasData seeds for "application" — see
    // Domain/Constants/StatusNames.cs.
    private const int ApplicationStatusId = 1;

    public async Task<PurchasedSiteDto> Handle(RequestPublicationCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdAsync(request.SiteId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        _ = await projectRepository.GetByIdForOwnerAsync(request.ProjectId, request.BuyerId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var wallet = await walletRepository.GetOrCreateAsync(request.BuyerId, cancellationToken);
        if (wallet.Balance < request.PriceFinal)
        {
            throw new ConflictException("Недостаточно средств на балансе. Пополните баланс, чтобы разместить заказ.");
        }

        var paymentSetting = await paymentSettingRepository.GetOrCreateAsync(
            request.InsuranceType, request.CheckUniqueness, request.IsUrgent, request.IsExpertArticle, cancellationToken);

        var order = new PurchasedSite
        {
            ProjectId = request.ProjectId,
            SiteId = request.SiteId,
            BuyerId = request.BuyerId,
            StatusId = ApplicationStatusId,
            TaskDescription = request.TaskDescription,
            PaymentSettingId = paymentSetting.Id,
            FinalPrice = request.PriceFinal,
            HasLinks = request.HasLinks,
        };

        if (request.HasLinks)
        {
            foreach (var link in request.Links)
            {
                order.Links.Add(new Link { Url = link.Url, Name = link.Text });
            }
        }

        await purchasedSiteRepository.AddAsync(order, cancellationToken);

        site.SoldCount++;
        await siteRepository.SaveChangesAsync(cancellationToken);
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

        wallet.Balance -= request.PriceFinal;
        await walletRepository.SaveChangesAsync(cancellationToken);
        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = request.BuyerId,
                Type = BalanceTransactionType.Purchase,
                Amount = -request.PriceFinal,
                Description = $"Заказ на площадку {site.Url}",
                PurchasedSiteId = order.Id,
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        await statsRefresher.RefreshWebmasterActiveSalesAsync(site.SellerId, cancellationToken);
        await statsRefresher.RefreshWebmasterRevenueAsync(site.SellerId, cancellationToken);
        await statsRefresher.RefreshOptimizatorOrderStatsAsync(cancellationToken);
        await statsRefresher.RefreshProjectSpendAsync(cancellationToken);
        await notificationPusher.NotifyOrderReceivedAsync(site.SellerId, site.Url, cancellationToken);

        await notificationRepository.AddAsync(
            new Notification { UserId = site.SellerId, Message = $"Новый заказ на площадку {site.Url}" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        var seller = await userRepository.GetByIdWithRolesAndProjectsAsync(site.SellerId, cancellationToken);
        var sellerPreference = await notificationPreferenceRepository.GetOrCreateAsync(site.SellerId, cancellationToken);
        if (seller != null && sellerPreference.EmailOnOrderUpdates)
        {
            await emailSender.SendAsync(
                seller.Email,
                $"Новый заказ на площадку {site.Url}",
                $"""
                 <p>Здравствуйте, {seller.Name}!</p>
                 <p>У вас новый заказ на размещение ссылки на площадке <b>{site.Url}</b> на сумму {request.PriceFinal} ₽.</p>
                 <p>Зайдите в личный кабинет, чтобы принять заказ в работу.</p>
                 """,
                cancellationToken);
        }

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Заявка на размещение создана" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        var created = await purchasedSiteRepository.GetByIdAsync(order.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(PurchasedSite), order.Id);
        return PurchasedSiteDto.FromEntity(created, request.BuyerId);
    }
}
