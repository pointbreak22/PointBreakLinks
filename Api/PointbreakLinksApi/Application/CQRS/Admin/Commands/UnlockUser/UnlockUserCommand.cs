using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Commands.UnlockUser;

// Clears a brute-force lockout (Identity.Application's LoginCommandHandler) early, for a user
// who doesn't want to wait out the 15-minute window and got in touch with support instead.
public record UnlockUserCommand(int TargetUserId) : IRequest<AdminUserDto>;
