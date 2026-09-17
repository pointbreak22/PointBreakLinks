using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword) : IRequest;
