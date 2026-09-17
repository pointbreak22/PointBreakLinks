using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
