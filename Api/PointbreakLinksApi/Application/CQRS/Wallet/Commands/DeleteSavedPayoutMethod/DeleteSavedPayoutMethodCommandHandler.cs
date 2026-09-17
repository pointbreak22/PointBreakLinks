using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Commands.DeleteSavedPayoutMethod;

public class DeleteSavedPayoutMethodCommandHandler(ISavedPayoutMethodRepository repository) : IRequestHandler<DeleteSavedPayoutMethodCommand>
{
    public async Task Handle(DeleteSavedPayoutMethodCommand request, CancellationToken cancellationToken)
    {
        // Scoped to the caller's own userId in the lookup itself, not checked afterwards — same
        // shape as PurchasedSite's buyer/seller-scoped reads, so this 404s (not 403s) on someone
        // else's method rather than leaking that a given id exists at all.
        var method = await repository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
                     ?? throw new NotFoundException("SavedPayoutMethod", request.Id);

        await repository.DeleteAsync(method, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
