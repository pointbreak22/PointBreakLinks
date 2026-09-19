using Application.CQRS.Admin.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Admin.Commands.SetUserBanned;

public class SetUserBannedCommandHandler(IUserRepository userRepository, ILogger<SetUserBannedCommandHandler> logger)
    : IRequestHandler<SetUserBannedCommand, AdminUserDto>
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

        logger.LogWarning(
            "User {TargetUserId} {Action} by admin {AdminUserId}.",
            request.TargetUserId, request.IsBanned ? "banned" : "unbanned", request.AdminUserId);

        return AdminUserDto.FromEntity(user);
    }
}
