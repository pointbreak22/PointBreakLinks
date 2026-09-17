using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Favorites.Queries.GetMyFavorites;

public record GetMyFavoritesQuery(int BuyerId, int Page, int PerPage) : IRequest<PagedResult<SiteDto>>;
