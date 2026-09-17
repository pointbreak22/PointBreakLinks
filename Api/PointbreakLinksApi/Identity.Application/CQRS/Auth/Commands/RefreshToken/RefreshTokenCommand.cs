using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
