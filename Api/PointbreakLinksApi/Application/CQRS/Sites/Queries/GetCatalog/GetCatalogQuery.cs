using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetCatalog;

// The public buyer-facing catalog (FOXLinks' SiteController::index / optimizator.vue's
// fetchAllSites) — every active listing, any seller, no ownership filter. The filter fields are
// new, not from FOXLinks — its own filter panels were entirely decorative (see
// Domain/Repositories/ISiteRepository.cs's SiteCatalogFilter comment).
public record GetCatalogQuery(
    int Page,
    int PerPage,
    int? TopicId = null,
    int? CountryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinIks = null,
    int? MinDr = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PagedResult<SiteDto>>;
