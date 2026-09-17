using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.AcceptOrder;

public record AcceptOrderCommand(int PurchasedSiteId, int WebmasterId) : IRequest<PurchasedSiteDto>;
