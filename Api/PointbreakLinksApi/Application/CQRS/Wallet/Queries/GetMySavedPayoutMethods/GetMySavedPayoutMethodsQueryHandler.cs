using Application.CQRS.Wallet.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMySavedPayoutMethods;

public class GetMySavedPayoutMethodsQueryHandler(ISavedPayoutMethodRepository repository)
    : IRequestHandler<GetMySavedPayoutMethodsQuery, IReadOnlyList<SavedPayoutMethodDto>>
{
    public async Task<IReadOnlyList<SavedPayoutMethodDto>> Handle(GetMySavedPayoutMethodsQuery request, CancellationToken cancellationToken)
    {
        var methods = await repository.GetByUserAsync(request.UserId, cancellationToken);
        return methods.Select(SavedPayoutMethodDto.FromEntity).ToList();
    }
}
