using MediatR;

namespace Application.CQRS.Favorites.Queries.GetFavoriteSiteIds;

public record GetFavoriteSiteIdsQuery(int BuyerId) : IRequest<IReadOnlyList<int>>;
