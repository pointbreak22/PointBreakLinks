using FluentValidation.TestHelper;
using Identity.Application.CQRS.Auth.Commands.Register;

namespace Application.Tests.Identity;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void BlankName_IsInvalid()
    {
        var result = _validator.TestValidate(new RegisterCommand("", "user@example.com", "Passw0rd1"));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void InvalidEmail_IsInvalid(string email)
    {
        var result = _validator.TestValidate(new RegisterCommand("User", email, "Passw0rd1"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("short1A")] // too short
    [InlineData("nouppercase1")] // no uppercase
    [InlineData("NoDigitsHere")] // no digit
    public void WeakPassword_IsInvalid(string password)
    {
        var result = _validator.TestValidate(new RegisterCommand("User", "user@example.com", password));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void ValidCommand_IsValid()
    {
        var result = _validator.TestValidate(new RegisterCommand("User", "user@example.com", "Passw0rd1"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
