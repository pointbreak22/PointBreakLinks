using MediatR;

namespace Application.CQRS.SavedSearches.Commands.DeleteSavedSearch;

public record DeleteSavedSearchCommand(int UserId, int Id) : IRequest;
