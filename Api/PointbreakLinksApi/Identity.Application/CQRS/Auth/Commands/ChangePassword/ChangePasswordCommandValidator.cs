using FluentValidation;
using Identity.Application.Common;

namespace Identity.Application.CQRS.Auth.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.NewPassword)
            .Must(PasswordPolicy.IsValid)
            .WithMessage("Пароль должен быть не короче 8 символов и содержать заглавную букву и цифру.");
    }
}
