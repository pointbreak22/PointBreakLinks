using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Commands.SetUserBanned;

public class SetUserBannedCommandHandler(IUserRepository userRepository) : IRequestHandler<SetUserBannedCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(SetUserBannedCommand request, CancellationToken cancellationToken)
    {
        if (request.IsBanned && request.AdminUserId == request.TargetUserId)
        {
            throw new ConflictException("Нельзя заблокировать самого себя.");
        }

        var user = await userRepository.GetByIdWithRolesAndProjectsAsync(request.TargetUserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), request.TargetUserId);

        user.IsBanned = request.IsBanned;
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminUserDto.FromEntity(user);
    }
}
