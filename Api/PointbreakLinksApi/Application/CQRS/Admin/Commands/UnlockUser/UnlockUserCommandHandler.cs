using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Commands.UnlockUser;

public class UnlockUserCommandHandler(IUserRepository userRepository) : IRequestHandler<UnlockUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRolesAndProjectsAsync(request.TargetUserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), request.TargetUserId);

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminUserDto.FromEntity(user);
    }
}
