using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ChangeEmail;

public record ChangeEmailCommand(int UserId, string NewEmail, string Password) : IRequest;
