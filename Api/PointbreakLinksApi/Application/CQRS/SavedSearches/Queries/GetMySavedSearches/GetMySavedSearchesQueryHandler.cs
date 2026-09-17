using Application.CQRS.SavedSearches.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.SavedSearches.Queries.GetMySavedSearches;

public class GetMySavedSearchesQueryHandler(ISavedSearchRepository savedSearchRepository)
    : IRequestHandler<GetMySavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    public async Task<IReadOnlyList<SavedSearchDto>> Handle(GetMySavedSearchesQuery request, CancellationToken cancellationToken)
    {
        var searches = await savedSearchRepository.GetByUserAsync(request.UserId, cancellationToken);
        return searches.Select(SavedSearchDto.FromEntity).ToList();
    }
}
