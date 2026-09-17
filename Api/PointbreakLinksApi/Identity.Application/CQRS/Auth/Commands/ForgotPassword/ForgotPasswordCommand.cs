using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;
