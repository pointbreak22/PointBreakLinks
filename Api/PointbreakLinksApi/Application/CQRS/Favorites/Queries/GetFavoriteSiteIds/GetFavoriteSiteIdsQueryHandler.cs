using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Favorites.Queries.GetFavoriteSiteIds;

public class GetFavoriteSiteIdsQueryHandler(IFavoriteSiteRepository favoriteSiteRepository) : IRequestHandler<GetFavoriteSiteIdsQuery, IReadOnlyList<int>>
{
    public Task<IReadOnlyList<int>> Handle(GetFavoriteSiteIdsQuery request, CancellationToken cancellationToken) =>
        favoriteSiteRepository.GetFavoriteSiteIdsAsync(request.BuyerId, cancellationToken);
}
