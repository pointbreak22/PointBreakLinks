using Application.CQRS.Support.Commands.SendSupportMessage;
using Application.CQRS.Support.Queries.GetMySupportThread;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// User-facing side of the support feature: one persistent thread per user with "the support
// team" (any admin/moderator — see SupportStaffController for their side). Backs the sidebar's
// "Обратная связь" link, previously a dead coming-soon stub.
[Authorize]
[Route("api/support")]
public class SupportController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("my-thread")]
    public async Task<IActionResult> GetMyThread()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMySupportThreadQuery(userId)));
    }

    [HttpPost("my-thread/messages")]
    public async Task<IActionResult> SendMyMessage([FromBody] SendSupportMessageRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var message = await mediator.Send(new SendSupportMessageCommand(userId, request.Text));
        return StatusCode(201, message);
    }
}

public record SendSupportMessageRequest(string Text);
