using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

public class TwoFactorEnabledHandler : AuthorizationHandler<TwoFactorEnabledRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TwoFactorEnabledRequirement requirement)
    {
        if (context.User.HasClaim("two_factor_enabled", "true"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
