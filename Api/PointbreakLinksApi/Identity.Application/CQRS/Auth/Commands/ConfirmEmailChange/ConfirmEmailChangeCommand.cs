using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(string Token) : IRequest;
