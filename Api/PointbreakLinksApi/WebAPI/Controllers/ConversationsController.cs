using Application.CQRS.Messages.Queries.GetConversations;
using Application.CQRS.Messages.Queries.GetUnreadMessageCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// The messages inbox: aggregates every order-chat (MessagesController is scoped to one order)
// into a per-user conversation list, one row per order that has at least one message.
[Authorize]
[Route("api/conversations")]
public class ConversationsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetConversationsQuery(userId)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetUnreadMessageCountQuery(userId)));
    }
}
