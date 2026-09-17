using Identity.Application.CQRS.Auth.Commands.Confirm2Fa;
using Identity.Application.CQRS.Auth.Commands.Disable2Fa;
using Identity.Application.CQRS.Auth.Commands.RegenerateBackupCodes;
using Identity.Application.CQRS.Auth.Commands.RevokeTrustedDevice;
using Identity.Application.CQRS.Auth.Commands.Setup2Fa;
using Identity.Application.CQRS.Auth.Queries.GetBackupCodesStatus;
using Identity.Application.CQRS.Auth.Queries.GetMyTrustedDevices;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Authenticated account-settings side of 2FA — generate a QR code, confirm it with a real code,
// or disable it (password re-check required). The login-time second step lives in
// AuthController (POST /api/auth/login/2fa), which is necessarily anonymous.
[Authorize]
[Route("api/auth/2fa")]
public class TwoFactorController(IMediator mediator) : ApiControllerBase
{
    private const string TrustedDeviceCookieName = "trusted_device";


    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            return Ok(await mediator.Send(new Setup2FaCommand(userId)));
        }
        catch (ConflictException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] Confirm2FaRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            var backupCodes = await mediator.Send(new Confirm2FaCommand(userId, request.Code));
            return Ok(new { message = "Двухфакторная аутентификация включена", backupCodes });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ConflictException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] Disable2FaRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            await mediator.Send(new Disable2FaCommand(userId, request.Password));
            Response.Cookies.Delete(TrustedDeviceCookieName);
            return Ok(new { message = "Двухфакторная аутентификация отключена" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [HttpGet("trusted-devices")]
    public async Task<ActionResult<object>> GetTrustedDevices()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyTrustedDevicesQuery(userId)));
    }

    [HttpDelete("trusted-devices/{id:int}")]
    public async Task<IActionResult> RevokeTrustedDevice(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new RevokeTrustedDeviceCommand(userId, id));
        return NoContent();
    }

    [HttpGet("backup-codes/status")]
    public async Task<ActionResult<object>> BackupCodesStatus()
    {
        var userId = GetCurrentUserId()!.Value;
        var remaining = await mediator.Send(new GetBackupCodesStatusQuery(userId));
        return Ok(new { remaining });
    }

    [HttpPost("backup-codes/regenerate")]
    public async Task<IActionResult> RegenerateBackupCodes([FromBody] RegenerateBackupCodesRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            var backupCodes = await mediator.Send(new RegenerateBackupCodesCommand(userId, request.Password));
            return Ok(new { backupCodes });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (ConflictException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}

public record Confirm2FaRequest(string Code);
public record Disable2FaRequest(string Password);
public record RegenerateBackupCodesRequest(string Password);
