using Application.Common;
using Application.CQRS.Admin.Commands.ApproveWithdrawal;
using Application.CQRS.Admin.Commands.RejectWithdrawal;
using Application.CQRS.Admin.Commands.SetUserBanned;
using Application.CQRS.Admin.Commands.SetUserRole;
using Application.CQRS.Admin.Commands.UnlockUser;
using Application.CQRS.Admin.DTOs;
using Application.CQRS.Admin.Queries.GetAdminProjects;
using Application.CQRS.Admin.Queries.GetAdminSystemLogs;
using Application.CQRS.Admin.Queries.GetAdminTransactions;
using Application.CQRS.Admin.Queries.GetDashboard;
using Application.CQRS.Admin.Queries.GetPendingWithdrawals;
using Application.CQRS.Admin.Queries.GetUsers;
using Application.CQRS.Sites.Commands.ResolveDispute;
using Application.CQRS.Sites.DTOs;
using Application.CQRS.Sites.Queries.GetDisputedOrders;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — FOXLinks has no admin backend at all (admin-users.vue etc. are local mock
// arrays, see Application/CQRS/Admin/DTOs/AdminUserDto.cs), so this is designed rather than
// ported. Kept to what's real: list users, change role, ban/unban.
[Authorize(Roles = RoleNames.Admin)]
[Authorize(Policy = "RequireTwoFactor")]
[Route("api/admin")]
public class AdminController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard() =>
        Ok(await mediator.Send(new GetDashboardQuery()));

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int perPage = 15, [FromQuery] string? search = null, [FromQuery] string? role = null) =>
        Ok(await mediator.Send(new GetUsersQuery(page, perPage, search, role)));

    [HttpGet("projects")]
    public async Task<ActionResult<PagedResult<AdminProjectDto>>> GetProjects([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetAdminProjectsQuery(page, perPage)));

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<AdminTransactionDto>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetAdminTransactionsQuery(page, perPage)));

    [HttpGet("system-logs")]
    public async Task<ActionResult<PagedResult<AdminSystemLogDto>>> GetSystemLogs([FromQuery] int page = 1, [FromQuery] int perPage = 20) =>
        Ok(await mediator.Send(new GetAdminSystemLogsQuery(page, perPage)));

    [HttpGet("disputes")]
    public async Task<ActionResult<PagedResult<DisputedOrderDto>>> GetDisputes([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetDisputedOrdersQuery(page, perPage)));

    [HttpPost("disputes/{id:int}/resolve")]
    public async Task<ActionResult<object>> ResolveDispute(int id, [FromBody] ResolveDisputeRequest request)
    {
        var order = await mediator.Send(new ResolveDisputeCommand(id, request.RefundBuyer));
        return Ok(new { message = request.RefundBuyer ? "Спор решён в пользу покупателя" : "Спор решён в пользу продавца", data = order });
    }

    [HttpPost("users/{id:int}/ban")]
    public async Task<ActionResult<object>> SetBanned(int id, [FromBody] SetBannedRequest request)
    {
        var adminId = GetCurrentUserId()!.Value;
        var user = await mediator.Send(new SetUserBannedCommand(adminId, id, request.IsBanned));
        return Ok(new { message = request.IsBanned ? "Пользователь заблокирован" : "Пользователь разблокирован", data = user });
    }

    [HttpPost("users/{id:int}/role")]
    public async Task<ActionResult<object>> SetRole(int id, [FromBody] SetRoleRequest request)
    {
        var user = await mediator.Send(new SetUserRoleCommand(id, request.Role));
        return Ok(new { message = "Роль обновлена", data = user });
    }

    [HttpPost("users/{id:int}/unlock")]
    public async Task<ActionResult<object>> Unlock(int id)
    {
        var user = await mediator.Send(new UnlockUserCommand(id));
        return Ok(new { message = "Блокировка снята", data = user });
    }

    [HttpGet("withdrawals")]
    public async Task<ActionResult<PagedResult<AdminWithdrawalRequestDto>>> GetWithdrawals([FromQuery] int page = 1, [FromQuery] int perPage = 15) =>
        Ok(await mediator.Send(new GetPendingWithdrawalsQuery(page, perPage)));

    [HttpPost("withdrawals/{id:int}/approve")]
    public async Task<ActionResult<object>> ApproveWithdrawal(int id)
    {
        await mediator.Send(new ApproveWithdrawalCommand(id));
        return Ok(new { message = "Заявка одобрена" });
    }

    [HttpPost("withdrawals/{id:int}/reject")]
    public async Task<ActionResult<object>> RejectWithdrawal(int id, [FromBody] RejectWithdrawalRequest request)
    {
        await mediator.Send(new RejectWithdrawalCommand(id, request.Comment));
        return Ok(new { message = "Заявка отклонена, средства возвращены" });
    }
}

public record RejectWithdrawalRequest(string? Comment);

public record SetBannedRequest(bool IsBanned);

public record SetRoleRequest(string Role);

public record ResolveDisputeRequest(bool RefundBuyer);
