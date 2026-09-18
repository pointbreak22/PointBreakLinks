using FluentValidation;

namespace Application.CQRS.Reviews.Commands.ReplyToReview;

public class ReplyToReviewCommandValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewCommandValidator()
    {
        RuleFor(c => c.Reply).NotEmpty().WithMessage("Ответ не может быть пустым.");
    }
}
