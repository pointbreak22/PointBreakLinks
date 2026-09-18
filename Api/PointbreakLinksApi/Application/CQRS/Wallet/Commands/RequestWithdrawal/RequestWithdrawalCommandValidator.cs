using FluentValidation;

namespace Application.CQRS.Wallet.Commands.RequestWithdrawal;

public class RequestWithdrawalCommandValidator : AbstractValidator<RequestWithdrawalCommand>
{
    public RequestWithdrawalCommandValidator()
    {
        RuleFor(c => c.Amount).GreaterThan(0m).WithMessage("Сумма вывода должна быть больше нуля.");
        RuleFor(c => c.PayoutDetails).NotEmpty().WithMessage("Укажите реквизиты для вывода средств.");
    }
}
