using Application.CQRS.Wallet.Commands.AddSavedPayoutMethod;
using FluentValidation.TestHelper;

namespace Application.Tests.WalletTests;

public class AddSavedPayoutMethodCommandValidatorTests
{
    private readonly AddSavedPayoutMethodCommandValidator _validator = new();

    [Fact]
    public void BlankLabel_IsInvalid()
    {
        var result = _validator.TestValidate(new AddSavedPayoutMethodCommand(UserId: 1, "", "card 1234"));
        result.ShouldHaveValidationErrorFor(c => c.Label);
    }

    [Fact]
    public void BlankDetails_IsInvalid()
    {
        var result = _validator.TestValidate(new AddSavedPayoutMethodCommand(UserId: 1, "My card", ""));
        result.ShouldHaveValidationErrorFor(c => c.Details);
    }

    [Fact]
    public void ValidCommand_IsValid()
    {
        var result = _validator.TestValidate(new AddSavedPayoutMethodCommand(UserId: 1, "My card", "card 1234"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
