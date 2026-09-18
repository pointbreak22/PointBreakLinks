using FluentValidation;
using Identity.Application.Common;

namespace Identity.Application.CQRS.Auth.Commands.Register;

// Previously had no input validation at all — a request with an empty name, malformed email, or
// a 1-character password would sail straight through to RegisterCommandHandler (which only
// checks for a duplicate email) and fail, if at all, only when EF Core's HasMaxLength(255)
// constraint on Name/Email raised a raw database error. Now caught here, before any DB access.
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Укажите имя.")
            .MaximumLength(255).WithMessage("Имя слишком длинное.");

        RuleFor(c => c.Email).NotEmpty().WithMessage("Укажите email.")
            .EmailAddress().WithMessage("Некорректный формат email.")
            .MaximumLength(255).WithMessage("Email слишком длинный.");

        RuleFor(c => c.Password)
            .Must(PasswordPolicy.IsValid)
            .WithMessage("Пароль должен быть не короче 8 символов и содержать заглавную букву и цифру.");
    }
}
