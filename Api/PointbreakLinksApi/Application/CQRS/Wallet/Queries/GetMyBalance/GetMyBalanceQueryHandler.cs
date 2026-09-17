using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyBalance;

public class GetMyBalanceQueryHandler(IWalletRepository walletRepository) : IRequestHandler<GetMyBalanceQuery, decimal>
{
    public async Task<decimal> Handle(GetMyBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.GetOrCreateAsync(request.UserId, cancellationToken);
        return wallet.Balance;
    }
}
