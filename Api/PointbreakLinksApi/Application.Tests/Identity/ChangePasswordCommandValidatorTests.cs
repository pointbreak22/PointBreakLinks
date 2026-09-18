using FluentValidation.TestHelper;
using Identity.Application.CQRS.Auth.Commands.ChangePassword;

namespace Application.Tests.Identity;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Theory]
    [InlineData("short1A")]
    [InlineData("nouppercase1")]
    [InlineData("NoDigitsHere")]
    public void WeakNewPassword_IsInvalid(string password)
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(UserId: 1, "OldPassw0rd", password));
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void StrongNewPassword_IsValid()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(UserId: 1, "OldPassw0rd", "NewPassw0rd"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
