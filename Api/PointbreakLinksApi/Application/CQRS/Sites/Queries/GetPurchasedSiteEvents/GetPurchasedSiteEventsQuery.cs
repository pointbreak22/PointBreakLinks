using MediatR;

namespace Application.CQRS.Sites.Queries.GetPurchasedSiteEvents;

public record GetPurchasedSiteEventsQuery(int PurchasedSiteId, int ViewerId) : IRequest<IReadOnlyList<PurchasedSiteEventDto>>;

public record PurchasedSiteEventDto(int Id, string Description, DateTime CreatedAt);
