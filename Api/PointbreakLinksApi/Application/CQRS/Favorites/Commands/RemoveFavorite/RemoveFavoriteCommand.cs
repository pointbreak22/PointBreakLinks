using MediatR;

namespace Application.CQRS.Favorites.Commands.RemoveFavorite;

public record RemoveFavoriteCommand(int BuyerId, int SiteId) : IRequest;
