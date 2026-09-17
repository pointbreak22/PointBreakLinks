using MediatR;

namespace Application.CQRS.Sites.Commands.UploadSiteScreenshot;

// Returns the PREVIOUS ScreenshotPath (or null) so the controller — which owns all disk I/O,
// same split as MessagesController — knows whether there's an old file to delete after the new
// one is safely on disk and the DB row is committed.
public record UploadSiteScreenshotCommand(int SiteId, int SellerId, string RelativePath) : IRequest<string?>;
