using MediatR;

namespace Application.CQRS.Sites.Commands.DeleteSiteScreenshot;

// Returns the removed path (or null if there was none) so the controller can delete the file.
public record DeleteSiteScreenshotCommand(int SiteId, int SellerId) : IRequest<string?>;
