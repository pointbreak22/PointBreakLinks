using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Commands.SetUserBanned;

public record SetUserBannedCommand(int AdminUserId, int TargetUserId, bool IsBanned) : IRequest<AdminUserDto>;
