using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Admin.Commands.UnlockUser;

public class UnlockUserCommandHandler(IUserRepository userRepository, ILogger<UnlockUserCommandHandler> logger)
    : IRequestHandler<UnlockUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRolesAndProjectsAsync(request.TargetUserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), request.TargetUserId);

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {TargetUserId} manually unlocked by an admin.", request.TargetUserId);

        return AdminUserDto.FromEntity(user);
    }
}
