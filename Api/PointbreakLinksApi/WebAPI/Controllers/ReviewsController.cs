using Application.Common;
using Application.CQRS.Reviews.Commands.CreateReview;
using Application.CQRS.Reviews.Commands.ReplyToReview;
using Application.CQRS.Reviews.DTOs;
using Application.CQRS.Reviews.Queries.GetSiteReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — not from FOXLinks.
[Authorize]
[Route("api")]
public class ReviewsController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("purchased-sites/{id:int}/review")]
    public async Task<ActionResult<object>> CreateReview(int id, [FromBody] CreateReviewRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var review = await mediator.Send(new CreateReviewCommand(id, userId, request.Rating, request.Comment));
        return StatusCode(201, new { message = "Отзыв добавлен", data = review });
    }

    [HttpGet("sites/{id:int}/reviews")]
    public async Task<ActionResult<PagedResult<SiteReviewDto>>> GetSiteReviews(int id, [FromQuery] int page = 1, [FromQuery] int perPage = 10) =>
        Ok(await mediator.Send(new GetSiteReviewsQuery(id, page, perPage)));

    [HttpPost("reviews/{id:int}/reply")]
    public async Task<ActionResult<object>> ReplyToReview(int id, [FromBody] ReplyToReviewRequest request)
    {
        var sellerId = GetCurrentUserId()!.Value;
        var review = await mediator.Send(new ReplyToReviewCommand(id, sellerId, request.Reply));
        return Ok(new { message = "Ответ добавлен", data = review });
    }
}

public record CreateReviewRequest(int Rating, string? Comment);
public record ReplyToReviewRequest(string Reply);
