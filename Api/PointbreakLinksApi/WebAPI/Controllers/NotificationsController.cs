using Application.Common;
using Application.CQRS.Notifications.Commands.MarkAllRead;
using Application.CQRS.Notifications.DTOs;
using Application.CQRS.Notifications.Queries.GetMyNotifications;
using Application.CQRS.Notifications.Queries.GetUnreadCount;
using Application.CQRS.NotificationPreferences.Commands.UpdateNotificationPreferences;
using Application.CQRS.NotificationPreferences.Queries.GetMyNotificationPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Persistent inbox backing the header's notification bell — rows are written by
// RequestPublicationCommandHandler, AcceptOrderCommandHandler, SendMessageCommandHandler and
// ApproveSiteCommandHandler/RejectSiteCommandHandler alongside their existing INotificationPusher
// (live SignalR toast) calls, so a notification survives being offline when it fired.
[Authorize]
[Route("api/notifications")]
public class NotificationsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyNotificationsQuery(userId, page, perPage)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetUnreadCountQuery(userId)));
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new MarkAllReadCommand(userId));
        return NoContent();
    }

    // Email opt-out only — never gates the in-app inbox above, see NotificationPreference's
    // comment on why.
    [HttpGet("preferences")]
    public async Task<ActionResult<object>> GetPreferences()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyNotificationPreferencesQuery(userId)));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<object>> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var updated = await mediator.Send(new UpdateNotificationPreferencesCommand(userId, request.EmailOnOrderUpdates, request.EmailOnDisputeUpdates));
        return Ok(updated);
    }
}

public record UpdateNotificationPreferencesRequest(bool EmailOnOrderUpdates, bool EmailOnDisputeUpdates);
