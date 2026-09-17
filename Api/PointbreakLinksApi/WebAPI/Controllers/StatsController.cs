using Application.CQRS.Stats.Queries.GetDynamicStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Ported from the inline `/stats` closure in FOXLinks' routes/api.php.
[Authorize]
[Route("api/stats")]
public class StatsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery(Name = "page_key")] string pageKey) =>
        Ok(await mediator.Send(new GetDynamicStatsQuery(pageKey)));
}
