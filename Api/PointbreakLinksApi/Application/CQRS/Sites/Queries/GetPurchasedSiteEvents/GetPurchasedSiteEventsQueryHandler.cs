using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetPurchasedSiteEvents;

// The viewer may be either side of the order — a buyer tracking their purchase or the seller
// fulfilling it — so ownership is checked by trying both lookups rather than a single-role
// check, same shape as PurchasedSiteDto.FromEntity's viewer-aware fields.
public class GetPurchasedSiteEventsQueryHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository)
    : IRequestHandler<GetPurchasedSiteEventsQuery, IReadOnlyList<PurchasedSiteEventDto>>
{
    public async Task<IReadOnlyList<PurchasedSiteEventDto>> Handle(GetPurchasedSiteEventsQuery request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.ViewerId, cancellationToken)
                    ?? await purchasedSiteRepository.GetByIdForSellerAsync(request.PurchasedSiteId, request.ViewerId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        var events = await purchasedSiteEventRepository.GetByPurchasedSiteAsync(order.Id, cancellationToken);
        return events
            .OrderBy(e => e.CreatedAt)
            .Select(e => new PurchasedSiteEventDto(e.Id, e.Description, e.CreatedAt))
            .ToList();
    }
}
