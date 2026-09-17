using Application.CQRS.Sites.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.CQRS.Sites.Commands.RequestPublication;

public record RequestedLink(string Text, string Url);

// The buyer's purchase flow (FOXLinks' SiteController::requestPublication /
// buy-miralinks-modal.vue's handleOrder). PriceFinal is trusted from the client the same way
// FOXLinks' RequestPublicationRequest does — the price is computed client-side from the same
// insurance/urgency/expert multipliers shown in the summary tab, no server-side price engine
// exists to recompute it against.
public record RequestPublicationCommand(
    int SiteId,
    int BuyerId,
    int ProjectId,
    bool HasLinks,
    IReadOnlyList<RequestedLink> Links,
    string TaskDescription,
    decimal PriceFinal,
    InsuranceType InsuranceType,
    bool CheckUniqueness,
    bool IsUrgent,
    bool IsExpertArticle) : IRequest<PurchasedSiteDto>;
