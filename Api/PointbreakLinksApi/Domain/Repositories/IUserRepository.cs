using Domain.Entities;

namespace Domain.Repositories;

// Business-side view of users — Admin's ban/role management and any "load a user for display"
// need. Auth-flow concerns (email lookup for login, registration, refresh tokens) live in
// Identity's own IUserRepository (Identity.Domain/Repositories) instead — same underlying
// table, different bounded context.
public interface IUserRepository
{
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    // Lightweight lookup for the public seller-profile page — no Roles/Projects join needed.
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Admin surface: Roles + Projects loaded (for the project-count column). search matches
    // Name/Email (case-insensitive, substring); role filters to users holding that exact role
    // name. Either/both may be null for no filtering.
    Task<(IReadOnlyList<User> Items, int Total)> GetAllPaginatedAsync(
        int page, int perPage, string? search = null, string? role = null, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAndProjectsAsync(int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
