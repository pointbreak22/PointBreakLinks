using Application.CQRS.Wallet.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;

public class AddSavedPayoutMethodCommandHandler(ISavedPayoutMethodRepository repository)
    : IRequestHandler<AddSavedPayoutMethodCommand, SavedPayoutMethodDto>
{
    public async Task<SavedPayoutMethodDto> Handle(AddSavedPayoutMethodCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || string.IsNullOrWhiteSpace(request.Details))
        {
            throw new ConflictException("Название и реквизиты обязательны.");
        }

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
