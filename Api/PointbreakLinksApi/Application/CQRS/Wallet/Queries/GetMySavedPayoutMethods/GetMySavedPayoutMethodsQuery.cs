using Application.CQRS.Wallet.DTOs;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMySavedPayoutMethods;

public record GetMySavedPayoutMethodsQuery(int UserId) : IRequest<IReadOnlyList<SavedPayoutMethodDto>>;
