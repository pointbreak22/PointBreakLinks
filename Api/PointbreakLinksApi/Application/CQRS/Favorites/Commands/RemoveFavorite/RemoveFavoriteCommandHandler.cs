using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Favorites.Commands.RemoveFavorite;

public class RemoveFavoriteCommandHandler(IFavoriteSiteRepository favoriteSiteRepository) : IRequestHandler<RemoveFavoriteCommand>
{
    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        await favoriteSiteRepository.RemoveAsync(request.BuyerId, request.SiteId, cancellationToken);
        await favoriteSiteRepository.SaveChangesAsync(cancellationToken);
    }
}
