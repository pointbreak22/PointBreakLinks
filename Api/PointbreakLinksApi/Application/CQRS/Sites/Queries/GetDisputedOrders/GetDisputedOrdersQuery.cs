using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetDisputedOrders;

public record GetDisputedOrdersQuery(int Page, int PerPage) : IRequest<PagedResult<DisputedOrderDto>>;
