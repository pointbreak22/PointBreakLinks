using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Commands.SetUserRole;

public class SetUserRoleCommandHandler(IUserRepository userRepository) : IRequestHandler<SetUserRoleCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRolesAndProjectsAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        var role = await userRepository.GetRoleByNameAsync(request.RoleName, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.Role), request.RoleName);

        user.Roles.Clear();
        user.Roles.Add(role);
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminUserDto.FromEntity(user);
    }
}
