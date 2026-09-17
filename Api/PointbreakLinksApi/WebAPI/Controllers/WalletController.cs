using Application.Common;
using Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;
using Application.CQRS.Wallet.Commands.DeleteSavedPayoutMethod;
using Application.CQRS.Wallet.Commands.RequestWithdrawal;
using Application.CQRS.Wallet.Commands.TopUpBalance;
using Application.CQRS.Wallet.DTOs;
using Application.CQRS.Wallet.Queries.GetMyBalance;
using Application.CQRS.Wallet.Queries.GetMySavedPayoutMethods;
using Application.CQRS.Wallet.Queries.GetMyTransactions;
using Application.CQRS.Wallet.Queries.GetMyWithdrawalRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — not from FOXLinks (see PROJECT_MAP.md's "Известные ограничения": no wallet
// table exists anywhere in the source, and its balance-topup-modal.vue is orphaned). Real
// balance ledger, enforced at spend time by RequestPublicationCommandHandler — see
// TopUpBalanceCommandHandler's comment for the one honest gap (no real payment processor).
[Authorize]
[Route("api/wallet")]
public class WalletController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> GetBalance()
    {
        var userId = GetCurrentUserId()!.Value;
        var balance = await mediator.Send(new GetMyBalanceQuery(userId));
        return Ok(new { balance });
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<BalanceTransactionDto>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyTransactionsQuery(userId, page, perPage)));
    }

    [HttpPost("topup")]
    public async Task<ActionResult<object>> TopUp([FromBody] TopUpRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var balance = await mediator.Send(new TopUpBalanceCommand(userId, request.Amount, request.PaymentMethod));
        return Ok(new { message = "Баланс пополнен", balance });
    }

    [HttpGet("withdrawals")]
    public async Task<ActionResult<PagedResult<WithdrawalRequestDto>>> GetMyWithdrawals([FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyWithdrawalRequestsQuery(userId, page, perPage)));
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<object>> RequestWithdrawal([FromBody] WithdrawRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var data = await mediator.Send(new RequestWithdrawalCommand(userId, request.Amount, request.PayoutDetails));
        return Ok(new { message = "Заявка на вывод средств создана", data });
    }

    [HttpGet("payout-methods")]
    public async Task<ActionResult<IReadOnlyList<SavedPayoutMethodDto>>> GetPayoutMethods()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMySavedPayoutMethodsQuery(userId)));
    }

    [HttpPost("payout-methods")]
    public async Task<ActionResult<SavedPayoutMethodDto>> AddPayoutMethod([FromBody] AddPayoutMethodRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var method = await mediator.Send(new AddSavedPayoutMethodCommand(userId, request.Label, request.Details));
        return StatusCode(201, method);
    }

    [HttpDelete("payout-methods/{id:int}")]
    public async Task<IActionResult> DeletePayoutMethod(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new DeleteSavedPayoutMethodCommand(id, userId));
        return NoContent();
    }
}

public record TopUpRequest(decimal Amount, string PaymentMethod);
public record WithdrawRequest(decimal Amount, string PayoutDetails);
public record AddPayoutMethodRequest(string Label, string Details);
