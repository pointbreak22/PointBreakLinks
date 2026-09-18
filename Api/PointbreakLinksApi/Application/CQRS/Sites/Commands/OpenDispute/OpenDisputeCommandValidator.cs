using FluentValidation;

namespace Application.CQRS.Sites.Commands.OpenDispute;

public class OpenDisputeCommandValidator : AbstractValidator<OpenDisputeCommand>
{
    public OpenDisputeCommandValidator()
    {
        RuleFor(c => c.Reason).NotEmpty().WithMessage("Укажите причину спора.");
    }
}
