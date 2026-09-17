using Application.Common;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.VerifySite;

public class VerifySiteCommandHandler(ISiteRepository siteRepository, ISiteVerificationService verificationService)
    : IRequestHandler<VerifySiteCommand, bool>
{
    public async Task<bool> Handle(VerifySiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.OwnerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        var verified = await verificationService.VerifyAsync(site.Url, site.VerificationToken, cancellationToken);
        site.IsVerified = verified;
        await siteRepository.SaveChangesAsync(cancellationToken);
        return verified;
    }
}
