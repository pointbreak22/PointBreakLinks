using Application.CQRS.Support.Commands.SendSupportReply;
using Application.CQRS.Support.Queries.GetSupportTicketMessages;
using Application.CQRS.Support.Queries.GetSupportTickets;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Staff side of the support feature — every ticket platform-wide, any admin/moderator can view
// and reply to any of them (no per-staff-member assignment, same "whoever's available answers"
// model as the rest of the moderation queue).
[Authorize(Roles = RoleNames.Moderator + "," + RoleNames.Admin)]
[Authorize(Policy = "RequireTwoFactor")]
[Route("api/admin/support")]
public class SupportStaffController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetSupportTicketsQuery(page, perPage)));

    [HttpGet("tickets/{id:int}/messages")]
    public async Task<IActionResult> GetTicketMessages(int id) =>
        Ok(await mediator.Send(new GetSupportTicketMessagesQuery(id)));

    [HttpPost("tickets/{id:int}/messages")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] SendSupportMessageRequest request)
    {
        var staffId = GetCurrentUserId()!.Value;
        var message = await mediator.Send(new SendSupportReplyCommand(id, staffId, request.Text));
        return StatusCode(201, message);
    }
}
