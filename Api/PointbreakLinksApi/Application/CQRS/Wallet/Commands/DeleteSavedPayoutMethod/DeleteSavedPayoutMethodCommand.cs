using MediatR;

namespace Application.CQRS.Wallet.Commands.DeleteSavedPayoutMethod;

public record DeleteSavedPayoutMethodCommand(int Id, int UserId) : IRequest;
