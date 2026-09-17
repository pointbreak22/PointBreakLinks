using Domain.Entities;

namespace Application.CQRS.Admin.DTOs;

public record AdminRoleDto(string Name, string DisplayName);

// FOXLinks' admin-users.vue is a local mock array (usersData) with no backing API at all —
// this is a new, real feature, not a port. Kept deliberately minimal: no balance column (no
// wallet/payments backend exists anywhere in this app) and no separate "moderation" user
// status (that concept doesn't exist on User — only IsBanned, which is real and enforced at
// login/refresh, see LoginCommandHandler/RefreshTokenCommandHandler).
public record AdminUserDto(
    int Id,
    string Name,
    string Email,
    IReadOnlyList<AdminRoleDto> Roles,
    int ProjectsCount,
    bool IsBanned,
    string CreatedAt,
    // Null unless a LoginCommandHandler lockout is currently in effect (a past-but-uncleared
    // timestamp doesn't count — matches LoginCommandHandler's own ">" check).
    string? LockedUntil)
{
    // Requires Roles and Projects loaded (see IUserRepository.GetAllPaginatedAsync /
    // GetByIdWithRolesAndProjectsAsync).
    public static AdminUserDto FromEntity(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Roles.Select(r => new AdminRoleDto(r.Name, r.DisplayName ?? r.Name)).ToList(),
        user.Projects.Count,
        user.IsBanned,
        user.CreatedAt.ToString("dd.MM.yyyy"),
        user.LockoutEndsAt is { } lockoutEndsAt && lockoutEndsAt > DateTime.UtcNow ? lockoutEndsAt.ToString("dd.MM.yyyy HH:mm") : null);
}
