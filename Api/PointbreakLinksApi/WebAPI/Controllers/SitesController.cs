using System.Globalization;
using Application.Common;
using Application.CQRS.Sites.Commands.AcceptOrder;
using Application.CQRS.Sites.Commands.CancelOrder;
using Application.CQRS.Sites.Commands.ConfirmPublished;
using Application.CQRS.Sites.Commands.CreateSite;
using Application.CQRS.Sites.Commands.DeactivateSite;
using Application.CQRS.Sites.Commands.DeclineOrder;
using Application.CQRS.Sites.Commands.DeleteSiteScreenshot;
using Application.CQRS.Sites.Commands.OpenDispute;
using Application.CQRS.Sites.Commands.ReactivateSite;
using Application.CQRS.Sites.Commands.RequestPublication;
using Application.CQRS.Sites.Commands.UpdateSite;
using Application.CQRS.Sites.Commands.UploadSiteScreenshot;
using Application.CQRS.Sites.Commands.VerifySite;
using Application.CQRS.Sites.DTOs;
using Application.CQRS.Sites.Queries.GetCatalog;
using Application.CQRS.Sites.Queries.GetCountries;
using Application.CQRS.Sites.Queries.GetMySites;
using Application.CQRS.Sites.Queries.GetProjectSites;
using Application.CQRS.Sites.Queries.GetPurchasedSiteEvents;
using Application.CQRS.Sites.Queries.GetSiteById;
using Application.CQRS.Sites.Queries.GetSiteScreenshotPath;
using Application.CQRS.Sites.Queries.GetTopics;
using Application.CQRS.Sites.Queries.GetWebmasterSales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers;

// Ported from FOXLinks' SiteController — seller-side listing management plus the buyer-facing
// catalog and purchase flow (Optimizator module).
[Authorize]
[Route("api/sites")]
public class SitesController(IMediator mediator, IWebHostEnvironment environment) : ApiControllerBase
{
    private const long MaxScreenshotBytes = 5 * 1024 * 1024;

    // Same "don't trust the client" reasoning as MessagesController's attachment allowlist —
    // extension decides both the check and the content-type served back later.
    private static readonly Dictionary<string, string> AllowedScreenshotTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<SiteDto>>> GetMySites([FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMySitesQuery(userId, page, perPage)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var site = await mediator.Send(new GetSiteByIdQuery(id, userId));
        return Ok(new { data = site });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] SiteRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var site = await mediator.Send(new CreateSiteCommand(
            request.Url, request.TopicId, request.Description, request.Price, request.Iks, request.Dr ?? 0, request.Traffic ?? 0, request.CountryId, userId));

        return StatusCode(201, new { message = "Площадка успешно добавлена", data = site });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] SiteRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var site = await mediator.Send(new UpdateSiteCommand(
            id, userId, request.Url, request.TopicId, request.Description, request.Price, request.Iks, request.Dr ?? 0, request.Traffic ?? 0, request.CountryId));

        return Ok(new { message = "Данные площадки обновлены", data = site });
    }

    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<object>> Verify(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var verified = await mediator.Send(new VerifySiteCommand(id, userId));
        return Ok(new
        {
            message = verified ? "Владение площадкой подтверждено" : "Не удалось найти код подтверждения на странице",
            data = new { verified },
        });
    }

    [HttpPost("{id:int}/screenshot")]
    [RequestSizeLimit(MaxScreenshotBytes)]
    public async Task<IActionResult> UploadScreenshot(int id, IFormFile file)
    {
        if (file.Length == 0)
        {
            return Problem(detail: "Файл пустой.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > MaxScreenshotBytes)
        {
            return Problem(detail: "Файл слишком большой — максимум 5 МБ.", statusCode: StatusCodes.Status400BadRequest);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedScreenshotTypes.ContainsKey(extension))
        {
            return Problem(detail: "Допустимы только изображения (JPG, PNG, WEBP).", statusCode: StatusCodes.Status400BadRequest);
        }

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "sites");
        Directory.CreateDirectory(uploadsDir);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"uploads/sites/{storedFileName}";
        await using (var stream = System.IO.File.Create(Path.Combine(uploadsDir, storedFileName)))
        {
            await file.CopyToAsync(stream);
        }

        var userId = GetCurrentUserId()!.Value;
        string? oldPath;
        try
        {
            oldPath = await mediator.Send(new UploadSiteScreenshotCommand(id, userId, relativePath));
        }
        catch
        {
            // The DB write failed (e.g. not the owner) — don't leave an orphaned file behind.
            System.IO.File.Delete(Path.Combine(uploadsDir, storedFileName));
            throw;
        }

        if (!string.IsNullOrEmpty(oldPath))
        {
            var oldFullPath = Path.Combine(webRoot, oldPath);
            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Delete(oldFullPath);
            }
        }

        return Ok(new { message = "Скриншот загружен" });
    }

    [HttpDelete("{id:int}/screenshot")]
    public async Task<IActionResult> DeleteScreenshot(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var oldPath = await mediator.Send(new DeleteSiteScreenshotCommand(id, userId));

        if (!string.IsNullOrEmpty(oldPath))
        {
            var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var oldFullPath = Path.Combine(webRoot, oldPath);
            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Delete(oldFullPath);
            }
        }

        return Ok(new { message = "Скриншот удалён" });
    }

    // Deliberately anonymous — a donor site's screenshot is public marketing content for the
    // catalog (any buyer needs to see it to decide whether to purchase), not private data the
    // way a chat attachment is. Avoids every catalog <img> needing Bearer-auth blob gymnastics
    // for something with no sensitivity to protect.
    [AllowAnonymous]
    [HttpGet("{id:int}/screenshot")]
    public async Task<IActionResult> GetScreenshot(int id)
    {
        var relativePath = await mediator.Send(new GetSiteScreenshotPathQuery(id));
        if (string.IsNullOrEmpty(relativePath))
        {
            return NotFound();
        }

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativePath);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var contentType = AllowedScreenshotTypes.GetValueOrDefault(Path.GetExtension(relativePath), "application/octet-stream");
        return PhysicalFile(fullPath, contentType);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new DeactivateSiteCommand(id, userId));
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new ReactivateSiteCommand(id, userId));
        return NoContent();
    }

    [HttpPost("{purchasedSiteId:int}/confirm-published")]
    public async Task<IActionResult> ConfirmPublished(int purchasedSiteId)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new ConfirmPublishedCommand(purchasedSiteId, userId));
        return Ok(new { message = "Статус подтвержден" });
    }

    [HttpGet("~/api/purchased-sites/{purchasedSiteId:int}/events")]
    public async Task<ActionResult<IReadOnlyList<PurchasedSiteEventDto>>> GetPurchasedSiteEvents(int purchasedSiteId)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetPurchasedSiteEventsQuery(purchasedSiteId, userId)));
    }

    [HttpGet("~/api/projects/{projectId:int}/sites")]
    public async Task<ActionResult<PagedResult<PurchasedSiteDto>>> GetProjectSites(int projectId, [FromQuery] int page = 1, [FromQuery] int perPage = 15)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetProjectSitesQuery(projectId, userId, page, perPage)));
    }

    [HttpGet("~/api/projects/{projectId:int}/sites/export")]
    public async Task<IActionResult> ExportProjectSites(int projectId)
    {
        var userId = GetCurrentUserId()!.Value;
        var sites = await mediator.Send(new GetProjectSitesQuery(projectId, userId, 1, int.MaxValue));

        var csv = CsvWriter.Write(
            ["ID заказа", "Площадка", "Задание", "Стоимость", "Статус", "Опубликовано", "Отзыв оставлен", "Обновлено"],
            sites.Items.Select(order => (IReadOnlyList<string>)
            [
                order.Id.ToString(),
                order.Site.Url,
                order.TaskDescription ?? string.Empty,
                order.PriceFinal.ToString("F2", CultureInfo.InvariantCulture),
                order.Status.Description,
                order.IsPublished ? "Да" : "Нет",
                order.HasReview ? "Да" : "Нет",
                order.UpdatedAt,
            ]));

        return File(csv, "text/csv", "project-sites.csv");
    }

    [HttpGet("~/api/topics")]
    public async Task<IActionResult> GetTopics() => Ok(await mediator.Send(new GetTopicsQuery()));

    [HttpGet("~/api/countries")]
    public async Task<IActionResult> GetCountries() => Ok(await mediator.Send(new GetCountriesQuery()));

    [HttpGet("~/api/webmaster/sales")]
    public async Task<ActionResult<PagedResult<PurchasedSiteDto>>> GetSales([FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetWebmasterSalesQuery(userId, page, perPage)));
    }

    [HttpGet("~/api/webmaster/sales/export")]
    public async Task<IActionResult> ExportSales()
    {
        var userId = GetCurrentUserId()!.Value;
        var sales = await mediator.Send(new GetWebmasterSalesQuery(userId, 1, int.MaxValue));

        var csv = CsvWriter.Write(
            ["ID заказа", "Площадка", "Задание", "Стоимость", "Статус", "Покупатель", "Опубликовано", "Обновлено"],
            sales.Items.Select(order => (IReadOnlyList<string>)
            [
                order.Id.ToString(),
                order.Site.Url,
                order.TaskDescription ?? string.Empty,
                order.PriceFinal.ToString("F2", CultureInfo.InvariantCulture),
                order.Status.Description,
                order.Buyer.Name,
                order.IsPublished ? "Да" : "Нет",
                order.UpdatedAt,
            ]));

        return File(csv, "text/csv", "sales.csv");
    }

    [HttpPost("~/api/webmaster/sales/{id:int}/accept")]
    public async Task<ActionResult<object>> AcceptOrder(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var order = await mediator.Send(new AcceptOrderCommand(id, userId));
        return Ok(new { message = "Заказ принят в работу", data = order });
    }

    [HttpPost("~/api/webmaster/sales/{id:int}/decline")]
    public async Task<ActionResult<object>> DeclineOrder(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var order = await mediator.Send(new DeclineOrderCommand(id, userId));
        return Ok(new { message = "Заказ отклонён, средства возвращены покупателю", data = order });
    }

    [HttpPost("~/api/purchased-sites/{id:int}/cancel")]
    public async Task<ActionResult<object>> CancelOrder(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var order = await mediator.Send(new CancelOrderCommand(id, userId));
        return Ok(new { message = "Заказ отменён, средства возвращены на баланс", data = order });
    }

    [HttpPost("~/api/purchased-sites/{id:int}/dispute")]
    public async Task<ActionResult<object>> OpenDispute(int id, [FromBody] OpenDisputeRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var order = await mediator.Send(new OpenDisputeCommand(id, userId, request.Reason));
        return Ok(new { message = "Спор открыт, администратор рассмотрит заказ", data = order });
    }

    [HttpGet("~/api/all-sites")]
    public async Task<ActionResult<PagedResult<SiteDto>>> GetCatalog(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] int? topicId = null,
        [FromQuery] int? countryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minIks = null,
        [FromQuery] int? minDr = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false) =>
        Ok(await mediator.Send(new GetCatalogQuery(page, perPage, topicId, countryId, minPrice, maxPrice, minIks, minDr, sortBy, sortDescending)));

    [HttpPost("{id:int}/request-publication")]
    public async Task<ActionResult<object>> RequestPublication(int id, [FromBody] RequestPublicationRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var order = await mediator.Send(new RequestPublicationCommand(
            id,
            userId,
            request.ProjectId,
            request.HasLinks,
            request.Links.Select(l => new RequestedLink(l.Text, l.Url)).ToList(),
            request.TaskDescription,
            request.PriceFinal,
            request.PaymentSettings.InsuranceType,
            request.PaymentSettings.CheckUniqueness,
            request.PaymentSettings.IsUrgent,
            request.PaymentSettings.IsExpertArticle));

        return StatusCode(201, new { message = "Заявка на размещение отправлена", data = order });
    }
}

public record SiteRequest(string Url, int TopicId, string? Description, decimal Price, int Iks, int? Dr, int? Traffic, int? CountryId);

public record RequestPublicationLinkRequest(string Text, string Url);

public record RequestPublicationSettingsRequest(Domain.Enums.InsuranceType InsuranceType, bool CheckUniqueness, bool IsUrgent, bool IsExpertArticle);

public record RequestPublicationRequest(
    int ProjectId,
    bool HasLinks,
    IReadOnlyList<RequestPublicationLinkRequest> Links,
    string TaskDescription,
    decimal PriceFinal,
    RequestPublicationSettingsRequest PaymentSettings);

public record OpenDisputeRequest(string Reason);
