using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
