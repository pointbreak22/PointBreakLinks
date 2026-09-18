using Application.CQRS.Wallet.DTOs;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;

public class AddSavedPayoutMethodCommandHandler(ISavedPayoutMethodRepository repository)
    : IRequestHandler<AddSavedPayoutMethodCommand, SavedPayoutMethodDto>
{
    public async Task<SavedPayoutMethodDto> Handle(AddSavedPayoutMethodCommand request, CancellationToken cancellationToken)
    {
        // Label/Details-not-empty enforced by AddSavedPayoutMethodCommandValidator.
        var method = new SavedPayoutMethod
        {
            UserId = request.UserId,
            Label = request.Label.Trim(),
            Details = request.Details.Trim(),
        };

        await repository.AddAsync(method, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return SavedPayoutMethodDto.FromEntity(method);
    }
}
