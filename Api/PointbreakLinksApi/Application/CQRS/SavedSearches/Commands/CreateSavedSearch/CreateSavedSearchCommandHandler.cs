using Application.CQRS.SavedSearches.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.SavedSearches.Commands.CreateSavedSearch;

public class CreateSavedSearchCommandHandler(ISavedSearchRepository savedSearchRepository)
    : IRequestHandler<CreateSavedSearchCommand, SavedSearchDto>
{
    public async Task<SavedSearchDto> Handle(CreateSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var search = new SavedSearch
        {
            UserId = request.UserId,
            TopicId = request.TopicId,
            CountryId = request.CountryId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            MinIks = request.MinIks,
            MinDr = request.MinDr,
        };

        await savedSearchRepository.AddAsync(search, cancellationToken);
        await savedSearchRepository.SaveChangesAsync(cancellationToken);

        // Re-fetch with Topic/Country included so the response carries display names, not just ids.
        var created = await savedSearchRepository.GetByIdAsync(search.Id, request.UserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(SavedSearch), search.Id);
        return SavedSearchDto.FromEntity(created);
    }
}
