using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

// Identity's own view of user access — Auth-flow methods only. Admin's user-list/ban/role
// management uses a separate, business-owned IUserRepository (Domain/Repositories, same table,
// different bounded context) instead of this one.
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
