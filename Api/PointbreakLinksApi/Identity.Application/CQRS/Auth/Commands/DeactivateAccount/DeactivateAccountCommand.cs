using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.DeactivateAccount;

public record DeactivateAccountCommand(int UserId, string Password) : IRequest;
