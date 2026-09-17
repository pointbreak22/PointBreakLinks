using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Disable2Fa;

public record Disable2FaCommand(int UserId, string Password) : IRequest;
