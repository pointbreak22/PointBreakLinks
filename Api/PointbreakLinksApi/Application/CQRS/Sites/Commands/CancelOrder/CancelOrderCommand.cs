using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.CancelOrder;

public record CancelOrderCommand(int PurchasedSiteId, int BuyerId) : IRequest<PurchasedSiteDto>;
