using Domain.Entities;

namespace Application.CQRS.Wallet.DTOs;

public record SavedPayoutMethodDto(int Id, string Label, string Details)
{
    public static SavedPayoutMethodDto FromEntity(SavedPayoutMethod method) => new(method.Id, method.Label, method.Details);
}
