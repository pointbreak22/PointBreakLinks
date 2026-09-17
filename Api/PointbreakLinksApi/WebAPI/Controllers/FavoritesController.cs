using Application.Common;
using Application.CQRS.Favorites.Commands.AddFavorite;
using Application.CQRS.Favorites.Commands.RemoveFavorite;
using Application.CQRS.Favorites.Queries.GetFavoriteSiteIds;
using Application.CQRS.Favorites.Queries.GetMyFavorites;
using Application.CQRS.Sites.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — FOXLinks' "Избранные площадки" links (both on the webmaster and optimizator
// sidebars) all pointed at /coming-soon; never actually built.
[Authorize]
[Route("api/favorites")]
public class FavoritesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SiteDto>>> GetMyFavorites([FromQuery] int page = 1, [FromQuery] int perPage = 15)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyFavoritesQuery(userId, page, perPage)));
    }

    [HttpGet("ids")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetFavoriteSiteIds()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetFavoriteSiteIdsQuery(userId)));
    }

    [HttpPost("{siteId:int}")]
    public async Task<IActionResult> AddFavorite(int siteId)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new AddFavoriteCommand(userId, siteId));
        return NoContent();
    }

    [HttpDelete("{siteId:int}")]
    public async Task<IActionResult> RemoveFavorite(int siteId)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new RemoveFavoriteCommand(userId, siteId));
        return NoContent();
    }
}
