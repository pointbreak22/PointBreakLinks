using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetWebmasterSales;

public record GetWebmasterSalesQuery(int WebmasterId, int Page, int PerPage) : IRequest<PagedResult<PurchasedSiteDto>>;
