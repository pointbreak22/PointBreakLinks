using FluentValidation.TestHelper;
using Identity.Application.CQRS.Auth.Commands.ResetPassword;

namespace Application.Tests.Identity;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void WeakNewPassword_IsInvalid()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("some-token", "weak"));
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void StrongNewPassword_IsValid()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("some-token", "NewPassw0rd"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
