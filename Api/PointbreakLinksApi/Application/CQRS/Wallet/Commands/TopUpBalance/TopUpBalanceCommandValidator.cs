using Domain.Constants;
using FluentValidation;

namespace Application.CQRS.Wallet.Commands.TopUpBalance;

public class TopUpBalanceCommandValidator : AbstractValidator<TopUpBalanceCommand>
{
    public TopUpBalanceCommandValidator()
    {
        RuleFor(c => c.Amount).GreaterThan(0m).WithMessage("Сумма пополнения должна быть больше нуля.");
        RuleFor(c => c.PaymentMethod).Must(PaymentMethodNames.All.Contains).WithMessage("Неизвестный способ оплаты.");
    }
}
