using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Authorization;

// A bare Forbid() from the default handler gives the client no way to tell "wrong role" apart
// from "right role, but 2FA is off" — the frontend needs that distinction to redirect to the
// profile's 2FA setup instead of just bouncing to a generic access-denied page (see the client's
// jwt-auth.interceptor.ts, which looks for `requiresTwoFactor` on a 403 body).
public class RequireTwoFactorResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var failedOnTwoFactor = authorizeResult.Forbidden
            && (authorizeResult.AuthorizationFailure?.FailedRequirements.Any(r => r is TwoFactorEnabledRequirement) ?? false);

        if (!failedOnTwoFactor)
        {
            await DefaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = "Для доступа к этому разделу необходимо включить двухфакторную аутентификацию.",
            Extensions = { ["requiresTwoFactor"] = true },
        });
    }
}
