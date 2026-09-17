using Application.CQRS.Messages.Commands.SendMessage;
using Application.CQRS.Messages.Commands.SendMessageAttachment;
using Application.CQRS.Messages.Queries.GetMessageAttachment;
using Application.CQRS.Messages.Queries.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Not present as a real backend feature in FOXLinks (message-chat-modal.vue there is hardcoded
// fake conversation data) — built for real here since the Message entity/table already existed
// from the initial scaffold. Scoped per order (purchasedSiteId), not a general inbox.
[Authorize]
[Route("api/purchased-sites/{purchasedSiteId:int}/messages")]
public class MessagesController(IMediator mediator, IWebHostEnvironment environment) : ApiControllerBase
{
    private const long MaxAttachmentBytes = 10 * 1024 * 1024;

    // Deliberately not trusting the browser-supplied Content-Type on upload (or letting the
    // client's original filename anywhere near the filesystem) — extension decides both the
    // allowlist check and the content-type actually served back later, closing off disguised-
    // executable uploads and path traversal in one place.
    private static readonly Dictionary<string, string> AllowedAttachmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".zip"] = "application/zip",
    };

    [HttpGet]
    public async Task<IActionResult> Get(int purchasedSiteId)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMessagesQuery(purchasedSiteId, userId)));
    }

    [HttpPost]
    public async Task<IActionResult> Send(int purchasedSiteId, [FromBody] SendMessageRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var message = await mediator.Send(new SendMessageCommand(purchasedSiteId, userId, request.Text));
        return StatusCode(201, message);
    }

    [HttpPost("attachment")]
    [RequestSizeLimit(MaxAttachmentBytes)]
    public async Task<IActionResult> SendAttachment(int purchasedSiteId, [FromForm] string? text, IFormFile file)
    {
        if (file.Length == 0)
        {
            return Problem(detail: "Файл пустой.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > MaxAttachmentBytes)
        {
            return Problem(detail: "Файл слишком большой — максимум 10 МБ.", statusCode: StatusCodes.Status400BadRequest);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedAttachmentTypes.TryGetValue(extension, out var contentType))
        {
            return Problem(detail: "Недопустимый тип файла.", statusCode: StatusCodes.Status400BadRequest);
        }

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "messages");
        Directory.CreateDirectory(uploadsDir);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsDir, storedFileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var userId = GetCurrentUserId()!.Value;
        var originalFileName = Path.GetFileName(file.FileName);
        if (originalFileName.Length > 255)
        {
            originalFileName = originalFileName[^255..];
        }

        var message = await mediator.Send(new SendMessageAttachmentCommand(
            purchasedSiteId, userId, text, $"uploads/messages/{storedFileName}", originalFileName, contentType));
        return StatusCode(201, message);
    }

    [HttpGet("{messageId:int}/attachment")]
    public async Task<IActionResult> DownloadAttachment(int purchasedSiteId, int messageId)
    {
        var userId = GetCurrentUserId()!.Value;
        var attachment = await mediator.Send(new GetMessageAttachmentQuery(purchasedSiteId, messageId, userId));

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, attachment.RelativePath);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        return PhysicalFile(fullPath, attachment.ContentType, attachment.FileName);
    }
}

public record SendMessageRequest(string Text);
