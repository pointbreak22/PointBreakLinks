using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.OpenDispute;

public record OpenDisputeCommand(int PurchasedSiteId, int BuyerId, string Reason) : IRequest<PurchasedSiteDto>;
