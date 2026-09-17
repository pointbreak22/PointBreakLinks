using Application.CQRS.SavedSearches.DTOs;
using MediatR;

namespace Application.CQRS.SavedSearches.Queries.GetMySavedSearches;

public record GetMySavedSearchesQuery(int UserId) : IRequest<IReadOnlyList<SavedSearchDto>>;
