using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.DeclineOrder;

public record DeclineOrderCommand(int PurchasedSiteId, int WebmasterId) : IRequest<PurchasedSiteDto>;
