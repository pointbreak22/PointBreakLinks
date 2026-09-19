using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Sites.Commands.OpenDispute;

// Buyer-side escalation for an order stuck in "work" — the seller accepted but is stalling or
// unresponsive, and there's no way to force a resolution otherwise (CancelOrderCommandHandler
// only covers "application", before the seller has been paid — see its comment on why "work" is
// messier). This doesn't resolve anything itself, just flags the order for an admin
// (ResolveDisputeCommandHandler) to look at and decide.
public class OpenDisputeCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository,
    ILogger<OpenDisputeCommandHandler> logger)
    : IRequestHandler<OpenDisputeCommand, PurchasedSiteDto>
{
    private const int WorkStatusId = 4;

    public async Task<PurchasedSiteDto> Handle(OpenDisputeCommand request, CancellationToken cancellationToken)
    {
        var order = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.BuyerId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        if (order.StatusId != WorkStatusId)
        {
            throw new ConflictException("Открыть спор можно только по заказу в статусе «В работе».");
        }

        if (order.IsDisputed)
        {
            throw new ConflictException("Спор по этому заказу уже открыт.");
        }

        // Reason-not-empty enforced by OpenDisputeCommandValidator.
        order.IsDisputed = true;
        order.DisputeReason = request.Reason;
        await purchasedSiteRepository.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Dispute opened on order {OrderId} by buyer {BuyerId}. Reason: {Reason}", order.Id, request.BuyerId, request.Reason);

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = order.Id, Description = $"Покупатель открыл спор: {request.Reason}" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        var updated = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.BuyerId, cancellationToken)
                      ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);
        return PurchasedSiteDto.FromEntity(updated, request.BuyerId);
    }
}
