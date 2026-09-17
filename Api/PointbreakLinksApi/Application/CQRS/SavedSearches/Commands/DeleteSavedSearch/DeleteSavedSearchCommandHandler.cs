using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.SavedSearches.Commands.DeleteSavedSearch;

public class DeleteSavedSearchCommandHandler(ISavedSearchRepository savedSearchRepository)
    : IRequestHandler<DeleteSavedSearchCommand>
{
    public async Task Handle(DeleteSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var search = await savedSearchRepository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
                     ?? throw new NotFoundException(nameof(SavedSearch), request.Id);

        await savedSearchRepository.DeleteAsync(search, cancellationToken);
        await savedSearchRepository.SaveChangesAsync(cancellationToken);
    }
}
