using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.UploadSiteScreenshot;

public class UploadSiteScreenshotCommandHandler(ISiteRepository siteRepository) : IRequestHandler<UploadSiteScreenshotCommand, string?>
{
    public async Task<string?> Handle(UploadSiteScreenshotCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.SellerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        var oldPath = site.ScreenshotPath;
        site.ScreenshotPath = request.RelativePath;
        await siteRepository.SaveChangesAsync(cancellationToken);

        return oldPath;
    }
}
