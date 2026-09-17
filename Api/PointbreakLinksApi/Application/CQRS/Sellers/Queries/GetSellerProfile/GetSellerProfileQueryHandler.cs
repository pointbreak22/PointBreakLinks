using Application.CQRS.Sellers.DTOs;
using Application.CQRS.Sites.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sellers.Queries.GetSellerProfile;

public class GetSellerProfileQueryHandler(
    IUserRepository userRepository,
    ISiteRepository siteRepository,
    ISiteReviewRepository siteReviewRepository) : IRequestHandler<GetSellerProfileQuery, SellerProfileDto>
{
    public async Task<SellerProfileDto> Handle(GetSellerProfileQuery request, CancellationToken cancellationToken)
    {
        var seller = await userRepository.GetByIdAsync(request.SellerId, cancellationToken)
                     ?? throw new NotFoundException("User", request.SellerId);

        // A banned account's storefront shouldn't stay browsable just because its listings were
        // deactivated separately — same "treat it as gone" reasoning as DeactivateAccountCommand.
        if (seller.IsBanned)
        {
            throw new NotFoundException("User", request.SellerId);
        }

        var sites = await siteRepository.GetActiveByUserAsync(request.SellerId, cancellationToken);
        var (averageRating, reviewsCount) = await siteReviewRepository.GetSummaryBySellerAsync(request.SellerId, cancellationToken);

        return SellerProfileDto.FromEntity(seller, averageRating, reviewsCount, sites.Select(SiteDto.FromEntity).ToList());
    }
}
