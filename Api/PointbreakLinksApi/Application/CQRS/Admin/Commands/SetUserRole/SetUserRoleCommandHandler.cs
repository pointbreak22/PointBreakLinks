using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Admin.Commands.SetUserRole;

public class SetUserRoleCommandHandler(IUserRepository userRepository, ILogger<SetUserRoleCommandHandler> logger)
    : IRequestHandler<SetUserRoleCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRolesAndProjectsAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        var role = await userRepository.GetRoleByNameAsync(request.RoleName, cancellationToken)
                   ?? throw new NotFoundException(nameof(Domain.Entities.Role), request.RoleName);

        var previousRoles = string.Join(", ", user.Roles.Select(r => r.Name));

        user.Roles.Clear();
        user.Roles.Add(role);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "User {UserId}'s role changed from [{PreviousRoles}] to {NewRole}.",
            request.UserId, previousRoles, request.RoleName);

        return AdminUserDto.FromEntity(user);
    }
}
