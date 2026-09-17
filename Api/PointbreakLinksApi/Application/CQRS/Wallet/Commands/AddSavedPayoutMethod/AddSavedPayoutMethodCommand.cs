using Application.CQRS.Wallet.DTOs;
using MediatR;

namespace Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;

public record AddSavedPayoutMethodCommand(int UserId, string Label, string Details) : IRequest<SavedPayoutMethodDto>;
