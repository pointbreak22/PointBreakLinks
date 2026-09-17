using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

// Backs the "RequireTwoFactor" policy stacked on admin/moderator controllers — an account with
// one of those roles but no 2FA enabled can still log in and use everything else (profile,
// wallet, projects), but not act as admin/moderator until it turns 2FA on.
public class TwoFactorEnabledRequirement : IAuthorizationRequirement;
