using MediatR;

namespace Application.CQRS.Sites.Commands.ConfirmPublished;

public record ConfirmPublishedCommand(int PurchasedSiteId, int SellerId) : IRequest;
