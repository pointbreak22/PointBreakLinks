using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Favorites.Commands.AddFavorite;

public class AddFavoriteCommandHandler(IFavoriteSiteRepository favoriteSiteRepository, ISiteRepository siteRepository)
    : IRequestHandler<AddFavoriteCommand>
{
    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        _ = await siteRepository.GetByIdAsync(request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Site", request.SiteId);

        // Idempotent — favoriting an already-favorited site is a no-op, not an error.
        if (await favoriteSiteRepository.ExistsAsync(request.BuyerId, request.SiteId, cancellationToken))
        {
            return;
        }

        await favoriteSiteRepository.AddAsync(new FavoriteSite { BuyerId = request.BuyerId, SiteId = request.SiteId }, cancellationToken);
        await favoriteSiteRepository.SaveChangesAsync(cancellationToken);
    }
}
