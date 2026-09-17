using Application.CQRS.Analytics.DTOs;
using Application.CQRS.Analytics.Queries.GetAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — FOXLinks' analytics.vue is 100% hardcoded Chart.js arrays with no backend at
// all (see PROJECT_MAP.md). Real per-user aggregation instead: any authenticated user, buyer or
// seller, can call this for their own activity — no role gate, since one account can hold both
// projects (buyer) and listed sites (seller) at once.
[Authorize]
[Route("api/analytics")]
public class AnalyticsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AnalyticsDto>> Get()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetAnalyticsQuery(userId)));
    }
}
