using Application.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.ConfirmPublished;

public class ConfirmPublishedCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    IDynamicStatsRefresher statsRefresher) : IRequestHandler<ConfirmPublishedCommand>
{
    public async Task Handle(ConfirmPublishedCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.SellerId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        order.IsPublished = true;
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshProjectWorkStatsAsync(cancellationToken);
        await statsRefresher.RefreshProjectPublishedStatsAsync(cancellationToken);

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = "Продавец подтвердил публикацию" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);
    }
}
