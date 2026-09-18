using Application.CQRS.Wallet.Commands.TopUpBalance;
using Domain.Constants;
using FluentValidation.TestHelper;

namespace Application.Tests.WalletTests;

public class TopUpBalanceCommandValidatorTests
{
    private readonly TopUpBalanceCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void NonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _validator.TestValidate(new TopUpBalanceCommand(UserId: 1, amount, PaymentMethodNames.VisaMastercard));
        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void UnknownPaymentMethod_IsInvalid()
    {
        var result = _validator.TestValidate(new TopUpBalanceCommand(UserId: 1, 500m, "bitcoin"));
        result.ShouldHaveValidationErrorFor(c => c.PaymentMethod);
    }

    [Fact]
    public void ValidCommand_IsValid()
    {
        var result = _validator.TestValidate(new TopUpBalanceCommand(UserId: 1, 500m, PaymentMethodNames.Mir));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
