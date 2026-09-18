using FluentValidation;

namespace Application.CQRS.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(c => c.Rating).InclusiveBetween(1, 5).WithMessage("Оценка должна быть от 1 до 5.");
    }
}
