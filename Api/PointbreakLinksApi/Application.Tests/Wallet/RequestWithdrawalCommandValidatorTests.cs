using Application.CQRS.Wallet.Commands.RequestWithdrawal;
using FluentValidation.TestHelper;

namespace Application.Tests.WalletTests;

public class RequestWithdrawalCommandValidatorTests
{
    private readonly RequestWithdrawalCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void NonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _validator.TestValidate(new RequestWithdrawalCommand(UserId: 1, amount, "card 1234"));
        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void BlankPayoutDetails_IsInvalid()
    {
        var result = _validator.TestValidate(new RequestWithdrawalCommand(UserId: 1, 100m, "  "));
        result.ShouldHaveValidationErrorFor(c => c.PayoutDetails);
    }

    [Fact]
    public void ValidCommand_IsValid()
    {
        var result = _validator.TestValidate(new RequestWithdrawalCommand(UserId: 1, 100m, "card 1234"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
