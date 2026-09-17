using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Setup2Fa;

public record Setup2FaCommand(int UserId) : IRequest<TwoFactorSetupDto>;
