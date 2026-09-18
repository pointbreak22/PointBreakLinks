using Application.CQRS.Reviews.Commands.CreateReview;
using FluentValidation.TestHelper;

namespace Application.Tests.Reviews;

public class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void RatingOutOfRange_IsInvalid(int rating)
    {
        var result = _validator.TestValidate(new CreateReviewCommand(PurchasedSiteId: 1, BuyerId: 1, rating, "Comment"));
        result.ShouldHaveValidationErrorFor(c => c.Rating);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void RatingInRange_IsValid(int rating)
    {
        var result = _validator.TestValidate(new CreateReviewCommand(PurchasedSiteId: 1, BuyerId: 1, rating, "Comment"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
