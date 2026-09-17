using MediatR;

namespace Application.CQRS.Favorites.Commands.AddFavorite;

public record AddFavoriteCommand(int BuyerId, int SiteId) : IRequest;
