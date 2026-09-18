using Application.CQRS.Reviews.Commands.ReplyToReview;
using FluentValidation.TestHelper;

namespace Application.Tests.Reviews;

public class ReplyToReviewCommandValidatorTests
{
    private readonly ReplyToReviewCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankReply_IsInvalid(string reply)
    {
        var result = _validator.TestValidate(new ReplyToReviewCommand(ReviewId: 1, SellerId: 1, reply));
        result.ShouldHaveValidationErrorFor(c => c.Reply);
    }

    [Fact]
    public void NonBlankReply_IsValid()
    {
        var result = _validator.TestValidate(new ReplyToReviewCommand(ReviewId: 1, SellerId: 1, "Спасибо за отзыв!"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
