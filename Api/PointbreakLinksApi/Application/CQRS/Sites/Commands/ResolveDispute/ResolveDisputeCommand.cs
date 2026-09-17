using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.ResolveDispute;

public record ResolveDisputeCommand(int PurchasedSiteId, bool RefundBuyer) : IRequest<PurchasedSiteDto>;
