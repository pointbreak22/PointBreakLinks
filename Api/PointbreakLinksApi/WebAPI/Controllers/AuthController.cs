using Application.CQRS.Wallet.Queries.GetMyBalance;
using Identity.Application.CQRS.Auth.Commands.ChangeEmail;
using Identity.Application.CQRS.Auth.Commands.ChangePassword;
using Identity.Application.CQRS.Auth.Commands.CompleteTwoFactorLogin;
using Identity.Application.CQRS.Auth.Commands.ConfirmEmailChange;
using Identity.Application.CQRS.Auth.Commands.DeactivateAccount;
using Identity.Application.CQRS.Auth.Commands.ForgotPassword;
using Identity.Application.CQRS.Auth.Commands.Login;
using Identity.Application.CQRS.Auth.Commands.Logout;
using Identity.Application.CQRS.Auth.Commands.Register;
using Identity.Application.CQRS.Auth.Commands.RefreshToken;
using Identity.Application.CQRS.Auth.Commands.ResetPassword;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Application.CQRS.Auth.Queries.GetCurrentUser;
using Identity.Application.CQRS.Auth.Queries.GetMyLoginHistory;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers;

// Auth flow ported 1:1 from FOXLinks' AuthController: JSON access token in the response body,
// refresh token in an httpOnly cookie so it never touches client-side JS/localStorage. Auth
// commands/queries live in Identity.Application (a separate bounded context/database schema,
// see PROJECT_MAP.md); balance is a business/Wallet concept, composed into the response here at
// the HTTP boundary — the one place combining both contexts for one payload is appropriate.
[Route("api/auth")]
public class AuthController(IMediator mediator, IConfiguration configuration) : ApiControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";
    private const string TrustedDeviceCookieName = "trusted_device";

    // Tighter than the global per-IP limit (see Program.cs) — no account exists yet to lock out
    // (register) or the endpoint deliberately always "succeeds" regardless of input (forgot-
    // password), so a normal request-rate cap is the only thing standing between this and being
    // used to spam signups or bomb an inbox with reset emails.
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await mediator.Send(new RegisterCommand(request.Name, request.Email, request.Password));
        return Ok(await WriteRefreshCookieAndBuildResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            Request.Cookies.TryGetValue(TrustedDeviceCookieName, out var trustedDeviceToken);
            var result = await mediator.Send(new LoginCommand(
                request.Email, request.Password, trustedDeviceToken, ClientIpAddress(), TruncatedUserAgent()));
            if (result.RequiresTwoFactor)
            {
                // No cookie set yet — real tokens are only issued once /login/2fa validates a
                // TOTP code against this ticket (see CompleteTwoFactorLoginCommand).
                return Ok(new { requiresTwoFactor = true, ticket = result.TwoFactorTicket });
            }

            return Ok(await WriteRefreshCookieAndBuildResponse(result.Auth!));
        }
        catch (AuthenticationException ex)
        {
            // ProblemDetails' `detail` field (not the `{ message }` shape used elsewhere in this
            // controller) — matches DomainExceptionHandler's shape so the client's single
            // extractErrorMessage() helper picks it up. Needed for real once a login/refresh
            // failure could mean something more specific than "bad credentials" — e.g. a banned
            // account (LoginCommandHandler/RefreshTokenCommandHandler).
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [AllowAnonymous]
    [HttpPost("login/2fa")]
    public async Task<IActionResult> CompleteTwoFactorLogin([FromBody] CompleteTwoFactorLoginRequest request)
    {
        try
        {
            var result = await mediator.Send(new CompleteTwoFactorLoginCommand(
                request.Ticket, request.Code, request.RememberDevice, TruncatedUserAgent(), ClientIpAddress()));
            return Ok(await WriteRefreshCookieAndBuildResponse(result));
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token not found" });
        }

        try
        {
            var result = await mediator.Send(new RefreshTokenCommand(refreshToken));
            // Same refresh-token value, just a later expiry — no need to rewrite the cookie's
            // value, only its Max-Age, so we still set it (harmless) but do not rotate it.
            return Ok(await WriteRefreshCookieAndBuildResponse(result));
        }
        catch (AuthenticationException ex)
        {
            // ProblemDetails' `detail` field (not the `{ message }` shape used elsewhere in this
            // controller) — matches DomainExceptionHandler's shape so the client's single
            // extractErrorMessage() helper picks it up. Needed for real once a login/refresh
            // failure could mean something more specific than "bad credentials" — e.g. a banned
            // account (LoginCommandHandler/RefreshTokenCommandHandler).
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            await mediator.Send(new LogoutCommand(refreshToken));
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return Ok(new { message = "Logged out" });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email));
        // Same response whether or not the email exists — see ForgotPasswordCommandHandler.
        return Ok(new { message = "Если такой email зарегистрирован, на него отправлена ссылка для сброса пароля." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
            return Ok(new { message = "Пароль обновлён" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [Authorize]
    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            await mediator.Send(new ChangeEmailCommand(userId, request.NewEmail, request.Password));
            return Ok(new { message = "Письмо с подтверждением отправлено на новый адрес" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [AllowAnonymous]
    [HttpPost("confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        try
        {
            await mediator.Send(new ConfirmEmailChangeCommand(request.Token));
            return Ok(new { message = "Email обновлён" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            await mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword));
            return Ok(new { message = "Пароль изменён" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [Authorize]
    [HttpPost("deactivate-account")]
    public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        try
        {
            await mediator.Send(new DeactivateAccountCommand(userId, request.Password));
            // Same cookie cleanup as Logout — the server-side refresh token row is already gone
            // (see DeactivateAccountCommandHandler), this just stops the client from resending it.
            Response.Cookies.Delete(RefreshTokenCookieName);
            Response.Cookies.Delete(TrustedDeviceCookieName);
            return Ok(new { message = "Аккаунт удалён" });
        }
        catch (AuthenticationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [Authorize]
    [HttpGet("login-history")]
    public async Task<ActionResult<IReadOnlyList<LoginHistoryEntryDto>>> LoginHistory()
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyLoginHistoryQuery(userId)));
    }

    [Authorize]
    [HttpGet("~/api/me")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await mediator.Send(new GetCurrentUserQuery(userId.Value));
        var balance = await mediator.Send(new GetMyBalanceQuery(userId.Value));
        return Ok(new { user.Id, user.Name, user.Email, user.Roles, user.TwoFactorEnabled, balance });
    }

    private async Task<object> WriteRefreshCookieAndBuildResponse(AuthResultDto result)
    {
        Response.Cookies.Append(RefreshTokenCookieName, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("Cookies:Secure", true),
            // Client and API run on different origins (different ports locally; likely
            // different subdomains in production too, same as MessengerAzure's split between
            // its static-hosted client and its App Service API) — SameSite=Lax is not sent on
            // cross-origin fetch/XHR at all, only on top-level navigation, which silently broke
            // session restore on every full page load. None requires Secure=true, which we
            // already set (and require HTTPS for in practice).
            SameSite = SameSiteMode.None,
            Expires = result.RefreshTokenExpiresAt,
            Path = "/",
        });

        if (result.TrustedDeviceToken != null)
        {
            Response.Cookies.Append(TrustedDeviceCookieName, result.TrustedDeviceToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = configuration.GetValue("Cookies:Secure", true),
                SameSite = SameSiteMode.None,
                Expires = result.TrustedDeviceTokenExpiresAt,
                Path = "/",
            });
        }

        // Balance lives in the business Wallet, not Identity's UserDto — composed in here since
        // this is the HTTP boundary, the one place combining both bounded contexts for a single
        // response is appropriate (see class comment).
        var balance = await mediator.Send(new GetMyBalanceQuery(result.User.Id));
        var user = new { result.User.Id, result.User.Name, result.User.Email, result.User.Roles, result.User.TwoFactorEnabled, balance };

        return new { access_token = result.AccessToken, user };
    }

    // Best-effort only, for "История входов"/TrustedDevice labels — never parsed or trusted for
    // any security decision.
    private string? ClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? TruncatedUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(200, userAgent.Length)];
    }
}

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record CompleteTwoFactorLoginRequest(string Ticket, string Code, bool RememberDevice = false);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record DeactivateAccountRequest(string Password);
public record ChangeEmailRequest(string NewEmail, string Password);
public record ConfirmEmailChangeRequest(string Token);
