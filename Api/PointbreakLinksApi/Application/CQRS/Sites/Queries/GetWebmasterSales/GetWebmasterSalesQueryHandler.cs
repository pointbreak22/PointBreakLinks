using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetWebmasterSales;

public class GetWebmasterSalesQueryHandler(IPurchasedSiteRepository purchasedSiteRepository)
    : IRequestHandler<GetWebmasterSalesQuery, PagedResult<PurchasedSiteDto>>
{
    public async Task<PagedResult<PurchasedSiteDto>> Handle(GetWebmasterSalesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await purchasedSiteRepository.GetSalesByWebmasterAsync(request.WebmasterId, request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(o => PurchasedSiteDto.FromEntity(o, request.WebmasterId)).ToList();
        return PagedResult<PurchasedSiteDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
