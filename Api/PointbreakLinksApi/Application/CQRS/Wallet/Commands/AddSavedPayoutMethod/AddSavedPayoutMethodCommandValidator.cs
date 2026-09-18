using FluentValidation;

namespace Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;

public class AddSavedPayoutMethodCommandValidator : AbstractValidator<AddSavedPayoutMethodCommand>
{
    public AddSavedPayoutMethodCommandValidator()
    {
        RuleFor(c => c.Label).NotEmpty().WithMessage("Название обязательно.");
        RuleFor(c => c.Details).NotEmpty().WithMessage("Реквизиты обязательны.");
    }
}
