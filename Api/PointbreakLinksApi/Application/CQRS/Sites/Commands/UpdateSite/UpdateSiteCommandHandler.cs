using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.UpdateSite;

public class UpdateSiteCommandHandler(ISiteRepository siteRepository) : IRequestHandler<UpdateSiteCommand, SiteDto>
{
    public async Task<SiteDto> Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.OwnerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        var url = UrlSanitizer.Sanitize(request.Url);
        if (await siteRepository.UrlExistsAsync(url, site.Id, cancellationToken))
        {
            throw new ConflictException("Эта площадка уже добавлена в систему.");
        }

        // Only the fields the edit form actually sends (url/topic/description/price/iks) —
        // dr/traffic/country aren't editable here, matching add-edit-site-modal.vue's form.
        // FOXLinks' SiteRepository::update() also wrote dr/traffic from the same request, which
        // would silently zero them out since this form never sends those fields; not replicated.
        site.Url = url;
        site.TopicId = request.TopicId;
        site.Description = request.Description;
        site.Price = request.Price;
        site.Iks = request.Iks;

        await siteRepository.SaveChangesAsync(cancellationToken);

        var updated = await siteRepository.GetByIdAsync(site.Id, cancellationToken)
                      ?? throw new NotFoundException("Site", site.Id);
        return SiteDto.FromEntity(updated);
    }
}
