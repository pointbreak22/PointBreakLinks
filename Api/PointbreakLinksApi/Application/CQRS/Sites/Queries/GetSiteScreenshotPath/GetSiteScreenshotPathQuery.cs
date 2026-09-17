using MediatR;

namespace Application.CQRS.Sites.Queries.GetSiteScreenshotPath;

// No ownership check — screenshots are public marketing content for the catalog, same
// reasoning as SitesController.GetScreenshot's [AllowAnonymous]. Returns null if the site has no
// screenshot; throws NotFoundException only if the site itself doesn't exist.
public record GetSiteScreenshotPathQuery(int SiteId) : IRequest<string?>;
