using Application.Common;
using Application.CQRS.Moderation.Commands.ApproveSite;
using Application.CQRS.Moderation.Commands.RejectSite;
using Application.CQRS.Moderation.DTOs;
using Application.CQRS.Moderation.Queries.GetModerationAuditLog;
using Application.CQRS.Moderation.Queries.GetPendingSites;
using Domain.Exceptions;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — the `moderator` role existed from FOXLinks' seeded roles table but had nothing
// to do anywhere in the source (no moderation UI, no backend). Real gap: a Site is created with
// IsActive=false pending review (see CreateSiteCommandHandler) but nothing ever reviewed it
// before this — moderation status was purely decorative. Admin can act here too, same as
// AdminController's own actions.
[Authorize(Roles = RoleNames.Moderator + "," + RoleNames.Admin)]
[Authorize(Policy = "RequireTwoFactor")]
[Route("api/moderation")]
public class ModerationController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("sites")]
    public async Task<ActionResult<PagedResult<PendingSiteDto>>> GetPendingSites([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetPendingSitesQuery(page, perPage)));

    [HttpGet("audit-log")]
    public async Task<ActionResult<PagedResult<ModerationAuditEntryDto>>> GetAuditLog([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetModerationAuditLogQuery(page, perPage)));

    [HttpPost("sites/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await mediator.Send(new ApproveSiteCommand(id, GetCurrentUserId()!.Value));
        return Ok(new { message = "Площадка одобрена" });
    }

    [HttpPost("sites/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectSiteRequest? request)
    {
        await mediator.Send(new RejectSiteCommand(id, GetCurrentUserId()!.Value, request?.Reason));
        return Ok(new { message = "Площадка отклонена" });
    }

    // Reuses the exact same handlers as the single-site actions above, one Send per id, so every
    // side effect (stats refresh, notifications, saved-search matching on approve, audit log)
    // fires the same way it would for an individual click. A site already handled by someone
    // else (or deleted) just falls into `failed` instead of aborting the whole batch.
    [HttpPost("sites/bulk-approve")]
    public async Task<IActionResult> BulkApprove(BulkModerationRequest request)
    {
        var moderatorId = GetCurrentUserId()!.Value;
        var (approved, failed) = await ApplyToEachAsync(request.SiteIds, id => mediator.Send(new ApproveSiteCommand(id, moderatorId)));
        return Ok(new { approved, failed });
    }

    [HttpPost("sites/bulk-reject")]
    public async Task<IActionResult> BulkReject(BulkRejectRequest request)
    {
        var moderatorId = GetCurrentUserId()!.Value;
        var (rejected, failed) = await ApplyToEachAsync(request.SiteIds, id => mediator.Send(new RejectSiteCommand(id, moderatorId, request.Reason)));
        return Ok(new { rejected, failed });
    }

    private static async Task<(int SucceededCount, List<int> Failed)> ApplyToEachAsync(IReadOnlyCollection<int> siteIds, Func<int, Task> action)
    {
        var failed = new List<int>();
        var succeededCount = 0;
        foreach (var id in siteIds)
        {
            try
            {
                await action(id);
                succeededCount++;
            }
            catch (NotFoundException)
            {
                failed.Add(id);
            }
        }

        return (succeededCount, failed);
    }
}

public record BulkModerationRequest(List<int> SiteIds);
public record BulkRejectRequest(List<int> SiteIds, string? Reason);
public record RejectSiteRequest(string? Reason);
