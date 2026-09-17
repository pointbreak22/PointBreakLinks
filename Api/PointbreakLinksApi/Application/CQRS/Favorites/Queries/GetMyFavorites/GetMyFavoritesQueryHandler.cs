using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Favorites.Queries.GetMyFavorites;

public class GetMyFavoritesQueryHandler(IFavoriteSiteRepository favoriteSiteRepository) : IRequestHandler<GetMyFavoritesQuery, PagedResult<SiteDto>>
{
    public async Task<PagedResult<SiteDto>> Handle(GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await favoriteSiteRepository.GetFavoriteSitesAsync(request.BuyerId, request.Page, request.PerPage, cancellationToken);
        return PagedResult<SiteDto>.Create(items.Select(SiteDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
