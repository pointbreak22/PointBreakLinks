using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetSiteScreenshotPath;

public class GetSiteScreenshotPathQueryHandler(ISiteRepository siteRepository) : IRequestHandler<GetSiteScreenshotPathQuery, string?>
{
    public async Task<string?> Handle(GetSiteScreenshotPathQuery request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdAsync(request.SiteId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        return site.ScreenshotPath;
    }
}
