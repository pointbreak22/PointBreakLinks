using Application.CQRS.SavedSearches.Commands.CreateSavedSearch;
using Application.CQRS.SavedSearches.Commands.DeleteSavedSearch;
using Application.CQRS.SavedSearches.DTOs;
using Application.CQRS.SavedSearches.Queries.GetMySavedSearches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module, not from FOXLinks — optimizator.vue's filter panels were entirely decorative (see
// ISiteRepository's SiteCatalogFilter comment: the real filter set here was built from scratch).
// Lets a buyer save a set of catalog filters and get notified the moment a newly approved
// listing matches them — see ApproveSiteCommandHandler.
[Authorize]
[Route("api/saved-searches")]
public class SavedSearchesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SavedSearchDto>>> GetMine()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMySavedSearchesQuery(userId)));
    }

    [HttpPost]
    public async Task<ActionResult<SavedSearchDto>> Create([FromBody] CreateSavedSearchRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var created = await mediator.Send(new CreateSavedSearchCommand(
            userId, request.TopicId, request.CountryId, request.MinPrice, request.MaxPrice, request.MinIks, request.MinDr));
        return StatusCode(201, created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new DeleteSavedSearchCommand(userId, id));
        return NoContent();
    }
}

public record CreateSavedSearchRequest(int? TopicId, int? CountryId, decimal? MinPrice, decimal? MaxPrice, int? MinIks, int? MinDr);
