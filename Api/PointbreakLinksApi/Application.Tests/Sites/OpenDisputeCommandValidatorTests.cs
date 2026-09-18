using Application.CQRS.Sites.Commands.OpenDispute;
using FluentValidation.TestHelper;

namespace Application.Tests.Sites;

public class OpenDisputeCommandValidatorTests
{
    private readonly OpenDisputeCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankReason_IsInvalid(string reason)
    {
        var result = _validator.TestValidate(new OpenDisputeCommand(PurchasedSiteId: 1, BuyerId: 1, reason));
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void NonBlankReason_IsValid()
    {
        var result = _validator.TestValidate(new OpenDisputeCommand(PurchasedSiteId: 1, BuyerId: 1, "Продавец не выходит на связь"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
