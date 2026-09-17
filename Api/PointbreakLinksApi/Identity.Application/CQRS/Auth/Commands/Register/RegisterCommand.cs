using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Register;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<AuthResultDto>;
