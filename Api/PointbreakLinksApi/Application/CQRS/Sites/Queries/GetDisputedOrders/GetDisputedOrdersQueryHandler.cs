using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetDisputedOrders;

public class GetDisputedOrdersQueryHandler(IPurchasedSiteRepository purchasedSiteRepository)
    : IRequestHandler<GetDisputedOrdersQuery, PagedResult<DisputedOrderDto>>
{
    public async Task<PagedResult<DisputedOrderDto>> Handle(GetDisputedOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await purchasedSiteRepository.GetDisputedAsync(request.Page, request.PerPage, cancellationToken);

        var dtos = items
            .Select(o => new DisputedOrderDto(o.Id, o.SiteUrl, o.BuyerName, o.SellerName, o.FinalPrice, o.Reason, o.UpdatedAt.ToString("dd.MM.yyyy HH:mm")))
            .ToList();

        return PagedResult<DisputedOrderDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
