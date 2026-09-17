namespace Identity.Domain.Constants;

// Ported 1:1 from FOXLinks' seeded `roles` table (2025_12_05_105222_create_roles_and_user_role_table.php).
// Kept as string constants rather than an enum because Role is still a DB-managed lookup
// table (admin can rename/add roles later) — these are just the well-known seed values
// application code needs to branch on. Lives in Identity, not the business Domain, since role
// membership is an access-control concern of the Identity bounded context — WebAPI's
// [Authorize(Roles = ...)] attributes reference this directly.
public static class RoleNames
{
    public const string Admin = "admin";
    public const string Moderator = "moderator";
    public const string Webmaster = "webmaster";
    public const string Universal = "universal";
    public const string Optimizer = "optimizer";

    // Assigned automatically on self-registration — mirrors AuthController::register() in FOXLinks.
    public const string DefaultOnRegister = Universal;
}
