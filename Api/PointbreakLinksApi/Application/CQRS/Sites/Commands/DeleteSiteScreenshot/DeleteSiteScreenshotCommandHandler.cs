using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.DeleteSiteScreenshot;

public class DeleteSiteScreenshotCommandHandler(ISiteRepository siteRepository) : IRequestHandler<DeleteSiteScreenshotCommand, string?>
{
    public async Task<string?> Handle(DeleteSiteScreenshotCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.SellerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        var oldPath = site.ScreenshotPath;
        site.ScreenshotPath = null;
        await siteRepository.SaveChangesAsync(cancellationToken);

        return oldPath;
    }
}
