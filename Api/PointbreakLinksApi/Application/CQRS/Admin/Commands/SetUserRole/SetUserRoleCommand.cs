using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Commands.SetUserRole;

// Single-role replace, not add/remove — matches admin-users.vue's single-select role field
// even though the schema underneath is many-to-many (role_user). Simpler admin UX, and
// nothing elsewhere in the app depends on a user carrying more than one role at once.
public record SetUserRoleCommand(int UserId, string RoleName) : IRequest<AdminUserDto>;
