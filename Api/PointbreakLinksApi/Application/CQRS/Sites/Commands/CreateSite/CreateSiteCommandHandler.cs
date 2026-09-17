using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.CreateSite;

public class CreateSiteCommandHandler(ISiteRepository siteRepository, IDynamicStatsRefresher statsRefresher)
    : IRequestHandler<CreateSiteCommand, SiteDto>
{
    public async Task<SiteDto> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var url = UrlSanitizer.Sanitize(request.Url);
        if (await siteRepository.UrlExistsAsync(url, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Эта площадка уже добавлена в систему.");
        }

        var site = new Site
        {
            Url = url,
            TopicId = request.TopicId,
            Description = request.Description,
            Price = request.Price,
            Iks = request.Iks,
            Dr = (byte)request.Dr,
            Traffic = request.Traffic,
            CountryId = request.CountryId,
            SellerId = request.SellerId,
            // Every new listing starts under moderation — mirrors FOXLinks' SiteStoreDTO,
            // which hardcodes status_id 2 ("На модерации") on create. IsActive=false until a
            // moderator approves it (Application/CQRS/Moderation) — FOXLinks set this true
            // immediately, which meant an unreviewed listing was already buyable in the catalog
            // the moment it was created; not replicated.
            StatusId = 2,
            IsActive = false,
            // Random per-listing token the seller publishes on their site to prove ownership —
            // see Application/CQRS/Sites/Commands/VerifySite.
            VerificationToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
        };

        await siteRepository.AddAsync(site, cancellationToken);
        await siteRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshSiteCountsAsync(request.SellerId, cancellationToken);

        var created = await siteRepository.GetByIdAsync(site.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Site), site.Id);
        return SiteDto.FromEntity(created);
    }
}
